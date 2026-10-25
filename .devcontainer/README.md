# Development container setup

VS Code waits for [post-create.sh](./post-create.sh) to finish before attaching
to a newly created container. Setup migrates the development and test databases,
generates UI settings, and configures the local PostgreSQL connection in VS Code.

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

Loopback origins remain allowed in the Development environment.
