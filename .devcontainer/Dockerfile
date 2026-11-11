FROM mcr.microsoft.com/devcontainers/dotnet:10.0

# Install playwright dependencies (MAKE SURE TO KEEP PLAYWRIGHT VERSIONS IN SYNC WITH .NET PROJECTS)
RUN mkdir /tmp/installplaywright && \
    cd /tmp/installplaywright && \
    dotnet new xunit && \
    dotnet add package Microsoft.Playwright -v 1.63.0 && \
    dotnet build && \
    pwsh ./bin/Debug/net10.0/playwright.ps1 install-deps

USER root
# Render PDF and DOCX resumes in the real worker hosted by acceptance tests.
RUN apt-get update && \
    apt-get install -y --no-install-recommends libreoffice-writer poppler-utils fonts-liberation && \
    rm -rf /var/lib/apt/lists/*

# Install Flyway database migration tool
RUN wget -qO /tmp/flyway-commandline-13.11.1-linux-x64.tar.gz https://download.red-gate.com/maven/release/com/redgate/flyway/flyway-commandline/13.11.1/flyway-commandline-13.11.1-linux-x64.tar.gz && \
    tar -xvzf /tmp/flyway-commandline-13.11.1-linux-x64.tar.gz -C /tmp && \
    mv /tmp/flyway-13.11.1 /opt/flyway && \
    chmod 755 /opt/flyway/flyway && \
    ln -s /opt/flyway/flyway /usr/local/bin/flyway && \
    rm /tmp/flyway-commandline-13.11.1-linux-x64.tar.gz

# Install the Google Cloud CLI used by the release pipeline
RUN wget -qO- https://packages.cloud.google.com/apt/doc/apt-key.gpg | gpg --dearmor -o /usr/share/keyrings/cloud.google.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/cloud.google.gpg] https://packages.cloud.google.com/apt cloud-sdk main" > /etc/apt/sources.list.d/google-cloud-sdk.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends google-cloud-cli=591.0.0-0 && \
    rm -rf /var/lib/apt/lists/*

# Install PostgreSQL client for database role provisioning and permission checks
RUN apt-get update && \
    apt-get install -y --no-install-recommends postgresql-client-16=16.15-0ubuntu0.24.04.1 && \
    rm -rf /var/lib/apt/lists/*

RUN wget -qO /usr/local/bin/cloud-sql-proxy https://storage.googleapis.com/cloud-sql-connectors/cloud-sql-proxy/v2.27.0/cloud-sql-proxy.linux.amd64 && \
    echo "39ed2abcaa073c3b4714585e36a904e2b1d92218c5e07c18224fb73ef4bedde5  /usr/local/bin/cloud-sql-proxy" | sha256sum --check && \
    chmod 755 /usr/local/bin/cloud-sql-proxy

# Install Playwright browsers
USER vscode
RUN pwsh /tmp/installplaywright/bin/Debug/net10.0/playwright.ps1 install

# Pre-create chat storage and CLI login mount points so new named volumes are owned by vscode.
RUN mkdir -p /home/vscode/.vscode-server/data/User \
    /home/vscode/.vscode-server/data/agentSessionData \
    /home/vscode/.copilot/session-state \
    /home/vscode/.config/gh \
    /home/vscode/.config/gcloud