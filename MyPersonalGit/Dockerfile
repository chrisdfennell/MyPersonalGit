# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyPersonalGit.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Run Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

LABEL org.opencontainers.image.source="https://github.com/ChrisDFennell/MyPersonalGit" \
      org.opencontainers.image.description="Self-hosted Git server"

WORKDIR /app

# Git is required for git http-backend
RUN apt-get update && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user and the repos directory
RUN groupadd -r appuser && useradd -r -g appuser -m appuser \
    && mkdir /repos && chown appuser:appuser /repos

COPY --from=build /app/publish .

# Configure credentials via environment variables at runtime:
#   docker run -e Git__Users__fennell=secret -e Git__RequireAuth=true ...
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

USER appuser

ENTRYPOINT ["dotnet", "MyPersonalGit.dll"]