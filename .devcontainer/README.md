# Development container setup

VS Code waits for [post-create.sh](./post-create.sh) to finish before attaching
to a newly created container. Setup migrates the development and test databases,
generates UI settings and API/UI launch profiles, and configures the local
PostgreSQL connection in VS Code.

Wait for **RLS setup complete. You can start working and debugging now.** in the
container setup output before starting the debugger. Then select **API + UI** in
Run and Debug and press **F5**.

If setup fails, resolve the reported error and rerun from the repository root:

```sh
bash .devcontainer/post-create.sh
```

The ready message is printed only after all setup steps succeed. Rebuild the
container to apply changes to its lifecycle configuration.

## API configuration

The API reads configuration only from environment variables and command-line
arguments. Command-line arguments take precedence. It does not load appsettings
JSON files or user secrets.

The container supplies the required `ConnectionStrings__RLS` environment variable.
For other settings, use double underscores for nested environment keys, such as
`Logging__LogLevel__Microsoft.AspNetCore=Warning` or
`Cors__AllowedOrigins__0=https://example.com`. The equivalent command-line options
are `--Logging:LogLevel:Microsoft.AspNetCore=Warning` and
`--Cors:AllowedOrigins:0=https://example.com`.

Compose sets `Cors__AllowedOrigins__0` from `UI_HTTP_URL` and
`Cors__AllowedOrigins__1` from `UI_HTTPS_URL` to match the UI launch profiles.
Loopback origins remain allowed in the Development environment. Rebuild the
container after changing its environment variables.

## Launch configuration

Set `API_HTTP_URL`, `API_HTTPS_URL`, `UI_HTTP_URL`, `UI_HTTPS_URL`, and
`ASPNETCORE_ENVIRONMENT` in [.env](./.env). URLs must be origins without paths
or trailing slashes. HTTP URLs use HTTP; HTTPS URLs use HTTPS.

Compose exports these values to the container. During setup,
[generate-launch-settings.cs](../.vscode/generate-launch-settings.cs)
generates local, Git-ignored API and UI launch settings with `http` and `https`
profiles, preserving Swagger and Blazor browser debugging support. VS Code's
API and UI debug configurations use `API_HTTP_URL` and `UI_HTTP_URL`.

After changing these values, rebuild the container to refresh its environment
and regenerate the settings. Do not edit generated launch settings directly.
