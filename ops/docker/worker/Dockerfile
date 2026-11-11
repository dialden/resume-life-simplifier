FROM mcr.microsoft.com/dotnet/runtime:10.0
LABEL org.opencontainers.image.source="https://github.com/dialden/resume-life-simplifier"
RUN apt-get update && \
    apt-get install -y --no-install-recommends libreoffice-writer poppler-utils fonts-liberation && \
    rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY . .
USER app
ENTRYPOINT ["dotnet", "DialDen.RLS.Worker.Root.dll"]
