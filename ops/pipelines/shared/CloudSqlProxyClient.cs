using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace DialDen.RLS.Pipelines;

public sealed class CloudSqlProxyClient(ProcessRunner processRunner, GoogleCloudClient secrets)
{
    public async Task MigrateDatabase(
        string connectionName, string database, string passwordSecret, string migrationsDirectory)
    {
        if (!Directory.Exists(migrationsDirectory) ||
            !Directory.EnumerateFiles(migrationsDirectory, "*.sql").Any())
        {
            throw new InvalidOperationException($"No exported SQL migrations found in '{migrationsDirectory}'.");
        }

        var configFile = Path.Combine(migrationsDirectory, "flyway-release.toml");
        await File.WriteAllTextAsync(configFile, """
            [flyway]
            locations = ["filesystem:."]
            failOnMissingLocations = true
            validateMigrationNaming = true
            validateOnMigrate = true
            cleanDisabled = true
            baselineOnMigrate = false
            outOfOrder = false

            [environments.default]
            connectRetries = 10
            """);
        var password = await secrets.ReadSecret(passwordSecret);
        await RunThroughProxy(connectionName, async port =>
        {
            await processRunner.Run("flyway",
                [
                    $"-configFiles={configFile}",
                    $"-url=jdbc:postgresql://127.0.0.1:{port}/{database}",
                    "-user=rls_migrator",
                    "migrate"
                ],
                new Dictionary<string, string> { ["FLYWAY_PASSWORD"] = password },
                workingDirectory: migrationsDirectory,
                removeEnvironmentPrefixes: ["FLYWAY_", "PG"]);
        });
        PipelineLog.Write($"Release migrations applied to database '{database}' through Cloud SQL Auth Proxy.");
    }

    public async Task VerifyDatabaseLogin(string connectionName, string database, string username, string passwordSecret)
    {
        var password = await secrets.ReadSecret(passwordSecret);
        await RunThroughProxy(connectionName, async port =>
        {
            var login = (await processRunner.Run("psql",
                ["--no-psqlrc", "--quiet", "--set=ON_ERROR_STOP=1", "--tuples-only", "--no-align",
                    "--host=127.0.0.1", $"--port={port}", $"--username={username}", $"--dbname={database}",
                    "--command=SELECT current_user"],
                new Dictionary<string, string> { ["PGPASSWORD"] = password },
                captureOutput: true, removeEnvironmentPrefixes: ["PG"])).Trim();
            if (login != username)
            {
                throw new InvalidOperationException($"Database login could not be verified for '{username}'.");
            }
        });
        PipelineLog.Write($"Database login verified for '{username}' on '{database}' through Cloud SQL Auth Proxy.");
    }

    public async Task EnsureApplicationDatabaseLogin(
        string connectionName, string database, string migratorPasswordSecret, string applicationPasswordSecret)
    {
        var migratorPassword = await secrets.ReadSecret(migratorPasswordSecret);
        var applicationPassword = await secrets.ReadSecret(applicationPasswordSecret);
        if (!Regex.IsMatch(applicationPassword, @"\A[A-F0-9]{64}\z"))
        {
            throw new InvalidOperationException(
                $"Application password secret '{applicationPasswordSecret}' is not in the generated format; " +
                "refusing to embed it in provisioning SQL.");
        }

        await RunThroughProxy(connectionName, async port =>
        {
            var sql = $"""
                DO $release$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rls_app') THEN
                        CREATE ROLE rls_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS
                            PASSWORD '{applicationPassword}';
                    END IF;
                END
                $release$;
                SELECT CASE
                    WHEN r.rolcanlogin AND NOT r.rolsuper AND NOT r.rolcreatedb AND NOT r.rolcreaterole
                        AND NOT r.rolreplication AND NOT r.rolbypassrls
                        AND NOT EXISTS (
                            SELECT 1 FROM pg_auth_members m WHERE m.member = r.oid
                        )
                    THEN 'valid' ELSE 'invalid'
                END
                FROM pg_roles r
                WHERE r.rolname = 'rls_app';
                """;
            var result = await processRunner.Run("psql",
                ["--no-psqlrc", "--quiet", "--set=ON_ERROR_STOP=1", "--tuples-only", "--no-align",
                    "--host=127.0.0.1", $"--port={port}", "--username=rls_migrator", $"--dbname={database}"],
                new Dictionary<string, string> { ["PGPASSWORD"] = migratorPassword },
                captureOutput: true, standardInput: sql);
            if (result.Trim() != "valid")
            {
                throw new InvalidOperationException(
                    "Database role 'rls_app' is missing or has elevated attributes or role memberships.");
            }
            var login = (await processRunner.Run("psql",
                ["--no-psqlrc", "--quiet", "--set=ON_ERROR_STOP=1", "--tuples-only", "--no-align",
                    "--host=127.0.0.1", $"--port={port}", "--username=rls_app", $"--dbname={database}",
                    "--command=SELECT current_user"],
                new Dictionary<string, string> { ["PGPASSWORD"] = applicationPassword },
                captureOutput: true)).Trim();
            if (login != "rls_app")
            {
                throw new InvalidOperationException("The restricted application database login could not be verified.");
            }
        });
        PipelineLog.Write($"Restricted application login verified on database '{database}' with no elevated role memberships.");
    }

    public async Task EnsureApplicationDatabasePermissions(
        string connectionName, string database, string migratorPasswordSecret, string applicationPasswordSecret)
    {
        var migratorPassword = await secrets.ReadSecret(migratorPasswordSecret);
        var applicationPassword = await secrets.ReadSecret(applicationPasswordSecret);
        await RunThroughProxy(connectionName, async port =>
        {
            await processRunner.Run("psql",
                ["--no-psqlrc", "--quiet", "--set=ON_ERROR_STOP=1",
                    "--host=127.0.0.1", $"--port={port}", "--username=rls_migrator", $"--dbname={database}"],
                new Dictionary<string, string> { ["PGPASSWORD"] = migratorPassword },
                standardInput: """
                    BEGIN;
                    DO $release$
                    BEGIN
                        EXECUTE format('GRANT CONNECT ON DATABASE %I TO rls_app', current_database());
                    END
                    $release$;
                    GRANT USAGE ON SCHEMA public, messaging, transport TO rls_app;
                    GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public, messaging, transport TO rls_app;
                    REVOKE ALL PRIVILEGES ON TABLE public.flyway_schema_history FROM rls_app;
                    GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public, messaging, transport TO rls_app;
                    GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA transport TO rls_app;
                    ALTER DEFAULT PRIVILEGES FOR ROLE rls_migrator IN SCHEMA public, messaging, transport
                        GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO rls_app;
                    ALTER DEFAULT PRIVILEGES FOR ROLE rls_migrator IN SCHEMA public, messaging, transport
                        GRANT USAGE, SELECT ON SEQUENCES TO rls_app;
                    ALTER DEFAULT PRIVILEGES FOR ROLE rls_migrator IN SCHEMA transport
                        GRANT EXECUTE ON FUNCTIONS TO rls_app;
                    COMMIT;
                    """,
                removeEnvironmentPrefixes: ["PG"]);

            var permissions = (await processRunner.Run("psql",
                ["--no-psqlrc", "--quiet", "--set=ON_ERROR_STOP=1", "--tuples-only", "--no-align",
                    "--host=127.0.0.1", $"--port={port}", "--username=rls_app", $"--dbname={database}"],
                new Dictionary<string, string> { ["PGPASSWORD"] = applicationPassword },
                captureOutput: true, standardInput: """
                    SELECT CASE WHEN current_user = 'rls_app'
                        AND has_database_privilege(current_database(), 'CONNECT')
                        AND NOT has_database_privilege(current_database(), 'CREATE')
                        AND has_schema_privilege('public', 'USAGE')
                        AND has_schema_privilege('messaging', 'USAGE')
                        AND has_schema_privilege('transport', 'USAGE')
                        AND NOT EXISTS (
                            SELECT 1 FROM pg_namespace
                            WHERE nspname IN ('public', 'messaging', 'transport')
                                AND has_schema_privilege(oid, 'CREATE'))
                        AND NOT EXISTS (
                            SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                            CROSS JOIN unnest(ARRAY['SELECT', 'INSERT', 'UPDATE', 'DELETE']) AS p(privilege)
                            WHERE n.nspname IN ('public', 'messaging', 'transport')
                                AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
                                AND c.oid <> 'public.flyway_schema_history'::regclass
                                AND (NOT has_table_privilege(c.oid, p.privilege)
                                    OR c.relowner = current_user::regrole
                                    OR has_table_privilege(c.oid, 'TRUNCATE,REFERENCES,TRIGGER')))
                        AND NOT EXISTS (
                            SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                            CROSS JOIN unnest(ARRAY['USAGE', 'SELECT']) AS p(privilege)
                            WHERE n.nspname IN ('public', 'messaging', 'transport')
                                AND c.relkind = 'S' AND NOT has_sequence_privilege(c.oid, p.privilege))
                        AND NOT EXISTS (
                            SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
                            WHERE n.nspname = 'transport' AND p.prokind = 'f'
                                AND NOT has_function_privilege(p.oid, 'EXECUTE'))
                        AND NOT has_table_privilege('public.flyway_schema_history',
                            'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER')
                    THEN 'valid' ELSE 'invalid' END;
                    """,
                removeEnvironmentPrefixes: ["PG"])).Trim();
            if (permissions != "valid")
            {
                throw new InvalidOperationException(
                    "Application database permissions are missing or exceed the runtime data-access privileges. " +
                    "Schema creation, table ownership, elevated table privileges and Flyway history access are not allowed.");
            }
        });
        PipelineLog.Write($"Application database permissions granted and verified on '{database}' for 'rls_app'.");
    }

    private async Task RunThroughProxy(string connectionName, Func<int, Task> action)
    {
        var port = FindFreePort();
        var startInfo = new ProcessStartInfo("cloud-sql-proxy")
        {
            WorkingDirectory = processRunner.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("--address=127.0.0.1");
        startInfo.ArgumentList.Add($"--port={port}");
        startInfo.ArgumentList.Add(connectionName);

        using var proxy = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Cloud SQL Auth Proxy.");
        var outputTask = proxy.StandardOutput.ReadToEndAsync();
        var errorTask = proxy.StandardError.ReadToEndAsync();
        try
        {
            await WaitForProxy(proxy, port, outputTask, errorTask);
            await action(port);
        }
        finally
        {
            if (!proxy.HasExited)
            {
                proxy.Kill(entireProcessTree: true);
                await proxy.WaitForExitAsync();
            }
        }
    }

    private static async Task WaitForProxy(Process proxy, int port, Task<string> outputTask, Task<string> errorTask)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (proxy.HasExited)
            {
                var output = await outputTask;
                var error = await errorTask;
                throw new InvalidOperationException(
                    $"Cloud SQL Auth Proxy exited with code {proxy.ExitCode}. {output}{error}".TrimEnd());
            }

            try
            {
                using var connection = new TcpClient();
                await connection.ConnectAsync(IPAddress.Loopback, port);
                return;
            }
            catch (SocketException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }
        }

        throw new TimeoutException("Cloud SQL Auth Proxy did not open its local listener within 60 seconds.");
    }

    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
