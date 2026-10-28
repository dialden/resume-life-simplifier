FROM mcr.microsoft.com/dotnet/aspnet:10.0
LABEL org.opencontainers.image.source="https://github.com/dialden/resume-life-simplifier"
WORKDIR /app
ENV RLS_API_URLS=http://+:8080
COPY . .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "DialDen.RLS.API.Root.dll"]
