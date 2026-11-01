FROM mcr.microsoft.com/dotnet/runtime:10.0
LABEL org.opencontainers.image.source="https://github.com/dialden/resume-life-simplifier"
WORKDIR /app
COPY . .
USER app
ENTRYPOINT ["dotnet", "DialDen.RLS.Worker.Root.dll"]
