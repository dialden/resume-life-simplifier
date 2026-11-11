# Development container setup

VS Code waits for [post-create.cs](./post-create.cs) to finish before attaching
to a newly created container. Setup creates an empty [devcontainer.env](./devcontainer.env)
if missing, preserving any existing contents, then migrates the development and test databases,
generates UI settings and API/UI launch profiles, and configures the local
PostgreSQL connection in VS Code.

[devcontainer.env](./devcontainer.env) is Git-ignored and supplies environment
variables only to the `dev` service. It does not configure Compose itself.
If upgrading an existing checkout, rename `.env.local` to `devcontainer.env`
before rebuilding to retain your local settings.

Wait for **RLS setup complete. You can start working and debugging now.** in the
container setup output before starting the debugger. Then select **API + Worker + UI** in
Run and Debug and press **F5**.

If setup fails, resolve the reported error and rerun from the repository root:

```sh
./.devcontainer/post-create.cs
```

The ready message is printed only after all setup steps succeed. Rebuild the
container to apply changes to its lifecycle configuration.

## CLI logins

The image includes Cloud SQL Auth Proxy 2.27.0, installed as `cloud-sql-proxy`
with its published SHA-256 checksum verified during the build. It uses
Application Default Credentials to establish authenticated connections to
Cloud SQL; it does not replace the database user's password. Installation does
not start the proxy or change local PostgreSQL connections.
PostgreSQL client 16 is also installed for secure SQL provisioning through the
proxy; it uses stdin and environment variables rather than command-line
passwords.

GitHub CLI and Google Cloud CLI logins are stored in the `gh-config` and
`gcloud-config` named volumes, so they survive container rebuilds. Docker
registry logins are kept by the Dev Containers credential helper on your
machine. To remove the saved CLI logins, delete those volumes or run
`gh auth logout` and `gcloud auth revoke --all`.

Run `ops/dev/authenticate.cs` from the repository root to sign in. It
logs in to GHCR through the GitHub CLI, then selects the Google Cloud project
from [config.json](../ops/pipelines/config.json). If needed, it signs in the
Google Cloud CLI and its Application Default Credentials together: open the
printed link, then paste the verification code into the terminal. Finally, it
checks that the project number matches the configuration.

The [run-in-authenticated-devcontainer](../.github/actions/run-in-authenticated-devcontainer/action.yml)
action logs out of GitHub, GHCR and Google Cloud when its command ends, even on
failure, so workflow credentials are not left in these volumes.

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

`RLS_WORKER_JobSites__Type` selects `real` (explicitly set by Compose) or `stubbed`.
Real mode discovers implementations in shipped `DialDen.RLS.JobSites.*.dll`
assemblies, currently Jobillico. Stubbed mode requires a `JobSites:Sites` list;
each entry specifies `Id`, `Name`, and `Url`, using environment variables such as
`RLS_WORKER_JobSites__Sites__0__Id` and `RLS_WORKER_JobSites__Sites__0__Url`.
This works for the normal Worker executable, including
external deployments, without test-time service replacements. In-process
stubbed tests read `STUBBED_EXT_SYSTEMS_RLS_WORKER_JobSites__Type` and
`STUBBED_EXT_SYSTEMS_RLS_WORKER_JobSites__Sites__<index>__Id`, `Name`, and `Url`,
supplied by Compose for two independent stub sites, before
the Worker's registrations run. No fixture override selects the provider.

`JOBILLICO_ENDPOINT` in [.env](./.env) supplies
`RLS_WORKER_Jobillico__Endpoint` (currently `https://www.jobillico.com/api/push`).
Real Jobillico publishing additionally requires `RLS_WORKER_Jobillico__Username`,
`RLS_WORKER_Jobillico__Password`, `RLS_WORKER_Jobillico__RecruiterId`, and
`RLS_WORKER_Jobillico__LocationId` in the Git-ignored [devcontainer.env](./devcontainer.env).
Post-creation setup creates this file empty if absent; it is not distributed through Git.
Add your values and rebuild the container to load them. Creating or editing the
file after container startup does not change the running container's environment.
The dev service loads it as an optional `env_file`. Shared settings remain in
the tracked [.env](./.env) and Compose `environment` section; `environment`
takes priority over matching `env_file` keys, so credentials are not duplicated there.
Compose explicitly
enables production (`Jobillico:UseProduction=true`). Publishing can create live
jobs. Rebuild after configuring credentials; never commit them.
The real-system test environment inherits the normal Jobillico configuration,
including its production endpoint and credentials. Stubbed tests use the generic
WireMock job sites instead.

## Ollama

The development image includes LibreOffice Writer, Poppler (`pdfinfo` and
`pdftoppm`) and Liberation fonts for resume rendering in the real Worker.
Rebuild the development container after changing these image dependencies.

Compose runs `ollama-init` to cache `qwen2.5vl:7b` in the `ollama_data`
volume before starting the selected Ollama service. Set `COMPOSE_PROFILES=gpu`
(default, requires NVIDIA Docker GPU support) or `COMPOSE_PROFILES=cpu` in
[.env](./.env). Select exactly one: both services bind the same host port.
This setting cannot be selected through [devcontainer.env](./devcontainer.env),
which is a service environment file, not a Compose configuration source.
Both profiles are available at `http://ollama:11434/` inside
the Compose network and `http://localhost:11434/` from the host.
GitHub Actions sets `COMPOSE_PROFILES=cpu` on the runner, overriding the
tracked `.env` value for its dev-container runs.
The commit, release and save-money workflows cache the Ollama model files in
GitHub Actions and restore them into the Compose volume before startup. A cache
miss downloads the model; only trusted main-branch runs save a cache. The jobs
set `COMPOSE_PROJECT_NAME` so the cache and dev container use the same volume.
The cache contains models only, not Ollama credentials or resume dumps. GitHub's default
repository cache limit is 10 GB, and the model is roughly 6 GB. Cache entries
are immutable and expire after inactivity; bump the version in the cache action's
key when the model or tag changes to prevent repeatedly downloading updated layers.
The model context window is configured by `OLLAMA_NUM_CTX` in [.env](./.env)
and passed as `Ollama:NumCtx` on each inference request. The dev-container
value is 65536; adjust it to fit the available GPU or system memory.
CPU inference can be much slower and may exceed the Worker's five-minute
inference timeout.

Both `dev` and `ollama-webui` wait for the selected service to be healthy.
Their dependency on the inactive profile is optional; Compose may report that
dependency as disabled. This requires Docker Compose 2.20 or newer.
When switching profiles, stop the old Ollama container before rebuilding the
dev container to avoid a port conflict or duplicate `ollama` network aliases.
From the host, in this directory, run:

```sh
docker compose --profile cpu --profile gpu stop ollama-cpu ollama-gpu
```

For the first rebuild after upgrading from the old single-service configuration,
stop the old `ollama` container as well. The shared `ollama_data` volume preserves
the downloaded model; do not remove volumes when switching profiles.

To inspect the exact prompts and rendered JPEG images sent to Ollama, add both
settings to the Git-ignored [devcontainer.env](./devcontainer.env), then rebuild the dev
container:

```dotenv
RLS_WORKER_Ollama__DumpDirectory=/tmp/rls-ollama-dumps
RLS_WORKER_Logging__LogLevel__DialDen=Debug
```

Use these Worker configuration keys, not `OLLAMA_DUMP_DIRECTORY` or
`OLLAMA_EXTRACTION_LOG_LEVEL`: service `env_file` values are passed into the
container, not used for Compose `${...}` interpolation. These keys are deliberately
absent from Compose's `environment` section so local values are not overridden.
The `DialDen` category prefix enables Debug logging for application code,
including extraction. Avoid dotted category names in environment keys: the
container startup shell can discard them before the Worker starts.
Dumping is disabled unless both settings enable it. Each inference
attempt writes a unique directory containing `request.json` (messages, model,
schema, options and ordered image filenames) and the original `page-*.jpg` bytes
sent to the model. The JSON references the image files instead of duplicating
their base64 contents. On Unix, each attempt directory is accessible only to its
owner. These files contain sensitive personal data: do not commit or share them.
Cleanup is manual; retries create separate dumps, and `/tmp` is not persistent
across container recreation. Dump-write failures fail the attempt rather than
silently skipping diagnostics. Disable the directory setting after debugging.

The dev container waits for Ollama to become healthy. Its health check uses
`ollama list` to verify that the server responds; it does not load the model
or run inference. The Ollama image does not include `curl`, so a health check
that invokes `curl` fails even when the server is running.

To inspect startup failures from the host:

```sh
docker compose ps
docker compose logs --tail 60 ollama-gpu
```

Use `ollama-cpu` instead when running the CPU profile.

After changing the health check, reopen or rebuild the dev container to apply
the Compose configuration. The cached model is preserved in its named volume.

## WireMock job-site stub

The `wiremock-job-site` Compose service backs the acceptance-test job-site
provider. The tests configure mappings through its admin API, and the in-process
test Worker calls the same service at `http://wiremock-job-site/`. From the
host, the service is available at `http://localhost:9095/`. The URL can be
overridden with `RLS_TEST_JobSites__WireMockUrl`. Rebuild or recreate the
devcontainer after changing Compose services.

## WireMock Ollama server

The separate `wiremock-ollama` service replaces only the Ollama server for
acceptance tests. The Worker, messaging, document rendering and persistence are
not replaced. Tests configure `/api/chat` responses through its admin API using
`RLS_TEST_ResumeAnalysis__WireMockUrl` (default `http://wiremock-ollama/`).
From the host, the server is available at `http://localhost:9096/`.

Compose sets `RLS_WORKER_Ollama__Endpoint=http://ollama:11434/` and
`RLS_WORKER_Ollama__Model=qwen2.5vl:7b`. The external-server-stubbed environment
overrides only the endpoint through
`STUBBED_EXT_SYSTEMS_RLS_WORKER_Ollama__Endpoint=http://wiremock-ollama/`.
The Worker registers the OllamaSharp adapter and consumes durable
`ApplicationReceived` events. In-process acceptance environments isolate both
the event topic and extraction queue; the real Worker is used in each environment.
Scenario setup resets the resume-analysis server to a successful empty result
unless a Given step configures contacts or a long response delay. Delayed requests
are cancelled by test Worker restart on the next reset. The in-process Worker also
stops on environment disposal; external Workers are managed by their host.

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

## Public job-posting address

The existing `UI_HTTP_URL` in [.env](./.env) also supplies the UI origin for
public job-posting links. No separate public-origin variable is needed.

Compose maps it to `RLS_API_PublicWebsite__BaseUrl`, including the
real/stubbed API test-environment variants. The API reads this as
`PublicWebsite:BaseUrl` and includes the resulting address in queued external
publication commands; the Worker does not need its own UI-origin setting.
`PublicWebsite:BaseUrl` is used only to construct public job-posting URLs; it
does not configure hosting or the UI's API connection. `UI_HTTP_URL` retains its
existing launch-profile and CORS uses. Recreate the devcontainer to refresh its
environment.

## Test hosting

Tests use environment variables rather than test appsettings JSON files.
`TEST_HOSTING_MODE` in [.env](./.env) selects hosting for both API and UI.
Compose maps it to the container variable `RLS_TEST_Hosting__Mode`:

- `InProcess` (the default when omitted): tests manage the hosts and discover
  their URLs. No URLs need to be configured. API-only scenarios do not start UI.
- `External`: set both `RLS_TEST_Hosting__APIUrl` and
  `RLS_TEST_Hosting__UIUrl` to absolute HTTP or HTTPS URLs. Tests do not start or
  stop these servers. Configure the external UI to target the external API.
  Resume-analysis scenarios that release a delayed response also require
  `RLS_TEST_Hosting__WorkerContainer` and Docker access to restart that Worker.

Compose passes these settings into the dev container. Rebuild the container
after editing the file. Hosting mode is independent of stubbed versus real
external systems. In-process API hosting continues to use
`RLS_TEST_CONNECTION_STRING` for the test database.
