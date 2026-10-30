# Development container setup

VS Code waits for [post-create.cs](./post-create.cs) to finish before attaching
to a newly created container. Setup migrates the development and test databases,
generates UI settings and API/UI launch profiles, and configures the local
PostgreSQL connection in VS Code.

Wait for **RLS setup complete. You can start working and debugging now.** in the
container setup output before starting the debugger. Then select **API + Worker + UI** in
Run and Debug and press **F5**.

If setup fails, resolve the reported error and rerun from the repository root:

```sh
./.devcontainer/post-create.cs
```

The ready message is printed only after all setup steps succeed. Rebuild the
container to apply changes to its lifecycle configuration.

`RLS_DEV_DATABASE` and `RLS_TEST_DATABASE` in [.env](./.env) name the development
and test databases. Compose passes them to both the dev container's migration
script and PostgreSQL's initialization script. The `.sql` initialization script
reads them with psql's `\getenv` and quotes them as identifiers, so no SQL
generation is needed.

## API configuration

The API reads application configuration only from environment variables prefixed
with `RLS_API_` and command-line arguments. The provider strips the prefix.
Command-line arguments take precedence and use unprefixed configuration keys.
It does not load appsettings JSON files or user secrets. Framework-native
hosting variables such as `ASPNETCORE_ENVIRONMENT` retain their standard names.

The container supplies the required `RLS_API_ConnectionStrings__RLS` environment variable.
For other settings, use double underscores for nested environment keys, such as
`RLS_API_Logging__LogLevel__Microsoft.AspNetCore=Warning` or
`RLS_API_Cors__AllowedOrigins__0=https://example.com`. The equivalent command-line options
are `--Logging:LogLevel:Microsoft.AspNetCore=Warning` and
`--Cors:AllowedOrigins:0=https://example.com`.

Compose sets `RLS_API_Cors__AllowedOrigins__0` from `UI_HTTP_URL` and
`RLS_API_Cors__AllowedOrigins__1` from `UI_HTTPS_URL` to match the UI launch profiles.
Loopback origins remain allowed in the Development environment. Rebuild the
container after changing its environment variables.

## Worker configuration

The Worker reads its configuration the same way, from environment variables
prefixed with `RLS_WORKER_` and command-line arguments. The container supplies
`RLS_WORKER_ConnectionStrings__RLS`, plus
`REAL_EXT_SYSTEMS_RLS_WORKER_ConnectionStrings__RLS` and
`STUBBED_EXT_SYSTEMS_RLS_WORKER_ConnectionStrings__RLS` for the Workers hosted
by the tests.

## Launch configuration

Set `API_HTTP_URL`, `API_HTTPS_URL`, `UI_HTTP_URL`, `UI_HTTPS_URL`,
`API_PROJECT_FILE`, `UI_PROJECT_FILE`, and `ASPNETCORE_ENVIRONMENT` in
[.env](./.env). Project-file values are filenames searched for under `src`.
URLs must be origins without paths or trailing slashes. HTTP URLs use HTTP;
HTTPS URLs use HTTPS.

Compose exports these values to the container. During setup,
[generate-launch-settings.cs](../.vscode/generate-launch-settings.cs)
generates local, Git-ignored API and UI launch settings with `http` and `https`
profiles, preserving Swagger and Blazor browser debugging support. VS Code's
API and UI debug configurations use `API_HTTP_URL` and `UI_HTTP_URL`.

The UI debugger explicitly selects the `http` launch profile and runs with
`--no-build`. Its `build ui` pre-launch task builds the app before debugging;
startup reuses that output instead of running a second build under the debugger.
If the pre-launch build fails, fix the build error rather than choosing
**Debug Anyway**, which could launch stale output.

After changing these values, rebuild the container to refresh its environment
and regenerate the settings. Do not edit generated launch settings directly.

## Test hosting

Tests use environment variables rather than test appsettings JSON files.
`TEST_HOSTING_MODE` in [.env](./.env) selects hosting for both API and UI.
Compose maps it to the container variable `RLS_TEST_Hosting__Mode`:

- `InProcess` (the default when omitted): tests manage the hosts and discover
  their URLs. No URLs need to be configured. API-only scenarios do not start UI.
- `External`: set both `RLS_TEST_Hosting__APIUrl` and
  `RLS_TEST_Hosting__UIUrl` to absolute HTTP or HTTPS URLs. Tests do not start or
  stop these servers. Configure the external UI to target the external API.

Compose passes these settings into the dev container. Rebuild the container
after editing the file. Hosting mode is independent of stubbed versus real
external systems. In-process API hosting continues to use
`RLS_TEST_CONNECTION_STRING` for the test database.
