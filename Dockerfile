# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyPersonalGit/MyPersonalGit.csproj", "MyPersonalGit/"]
RUN dotnet restore "MyPersonalGit/MyPersonalGit.csproj"
COPY MyPersonalGit/ MyPersonalGit/
RUN dotnet publish "MyPersonalGit/MyPersonalGit.csproj" -c Release -o /app/publish

# Run Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

LABEL org.opencontainers.image.source="https://github.com/ChrisDFennell/MyPersonalGit" \
      org.opencontainers.image.description="Self-hosted Git server"

WORKDIR /app

# Git is required for git http-backend, docker.io for CI/CD workflow runner
# gosu allows dropping from root to appuser after fixing permissions
RUN apt-get update && apt-get install -y --no-install-recommends git ca-certificates docker.io gosu \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user and the repos/data directories
# Add appuser to docker group for socket access
RUN groupadd -r appuser && useradd -r -g appuser -m appuser \
    && usermod -aG docker appuser \
    && mkdir -p /repos && chown appuser:appuser /repos \
    && mkdir -p /data && chown appuser:appuser /data

COPY --from=build /app/publish .
COPY entrypoint.sh /app/entrypoint.sh
RUN sed -i 's/\r$//' /app/entrypoint.sh && chmod +x /app/entrypoint.sh

# Configure credentials via environment variables at runtime:
#   docker run -e Git__Users__fennell=secret -e Git__RequireAuth=true ...
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ConnectionStrings__Default="Data Source=/data/mypersonalgit.db"
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

# Run as root so entrypoint can fix bind mount permissions, then drop to appuser
ENTRYPOINT ["/app/entrypoint.sh"]
