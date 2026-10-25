#!/usr/bin/env bash
set -Eeuo pipefail

trap 'printf "\nRLS setup failed. Fix the error above and rerun: bash .devcontainer/post-create.sh\nDo not start debugging until setup succeeds.\n" >&2' ERR

cd "$(dirname "${BASH_SOURCE[0]}")/.."

printf '\nRLS development environment setup is running. Please wait before starting the debugger.\n\n'

printf '[1/3] Migrating development and test databases...\n'
dotnet run ops/database/migrate.cs

printf '\n[2/3] Generating UI settings...\n'
dotnet run ops/ui/generate-settings.cs

printf '\n[3/3] Configuring the VS Code PostgreSQL connection...\n'
dotnet run .vscode/configure-postgres-connection.cs

printf '\n============================================================\n'
printf 'RLS setup complete. You can start working and debugging now.\n'
printf 'Select "API + UI" in Run and Debug, then press F5.\n'
printf '============================================================\n'
