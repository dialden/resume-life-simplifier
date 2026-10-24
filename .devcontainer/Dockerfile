FROM mcr.microsoft.com/devcontainers/dotnet:2-10.0-noble

# Install playwright dependencies (MAKE SURE TO KEEP PLAYWRIGHT VERSIONS IN SYNC WITH .NET PROJECTS)
RUN mkdir /tmp/installplaywright && \
    cd /tmp/installplaywright && \
    dotnet new xunit && \
    dotnet add package Microsoft.Playwright -v 1.63.0 && \
    dotnet build && \
    pwsh ./bin/Debug/net10.0/playwright.ps1 install-deps

USER root
# Install Flyway database migration tool
RUN wget -qO /tmp/flyway-commandline-13.11.1-linux-x64.tar.gz https://download.red-gate.com/maven/release/com/redgate/flyway/flyway-commandline/13.11.1/flyway-commandline-13.11.1-linux-x64.tar.gz && \
    tar -xvzf /tmp/flyway-commandline-13.11.1-linux-x64.tar.gz -C /tmp && \
    mv /tmp/flyway-13.11.1 /opt/flyway && \
    chmod 755 /opt/flyway/flyway && \
    ln -s /opt/flyway/flyway /usr/local/bin/flyway && \
    rm /tmp/flyway-commandline-13.11.1-linux-x64.tar.gz

# Install Playwright browsers
USER vscode
RUN pwsh /tmp/installplaywright/bin/Debug/net10.0/playwright.ps1 install

# Pre-create chat storage mount points so new named volumes are owned by vscode.
RUN mkdir -p /home/vscode/.vscode-server/data/User \
    /home/vscode/.vscode-server/data/agentSessionData \
    /home/vscode/.copilot/session-state