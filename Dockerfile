# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["MyPersonalGit/MyPersonalGit.csproj", "MyPersonalGit/"]
RUN dotnet restore "MyPersonalGit/MyPersonalGit.csproj"
COPY MyPersonalGit/ MyPersonalGit/
RUN dotnet publish "MyPersonalGit/MyPersonalGit.csproj" -c Release -o /app/publish -maxcpucount:1 -p:RunAnalyzers=false

# Run Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

LABEL org.opencontainers.image.source="https://github.com/ChrisDFennell/MyPersonalGit" \
      org.opencontainers.image.description="Self-hosted Git server"

WORKDIR /app

# Git is required for git http-backend, docker.io for CI/CD workflow runner
# gosu allows dropping from root to appuser after fixing permissions
# nodejs/npm for typescript-language-server, python3 for python-lsp-server
RUN apt-get update && apt-get install -y --no-install-recommends \
        git git-lfs ca-certificates docker.io gosu \
        nodejs npm python3 python3-pip curl \
    && git lfs install \
    && npm install -g typescript typescript-language-server \
        vscode-langservers-extracted yaml-language-server \
        bash-language-server dockerfile-language-server-nodejs \
    && pip3 install --break-system-packages python-lsp-server debugpy \
    && rm -rf /var/lib/apt/lists/* /root/.npm /root/.cache

# .NET 8 SDK (needed by OmniSharp C# language server)
RUN curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh

# Go + gopls language server
RUN ARCH=$(dpkg --print-architecture) && \
    curl -sSL "https://go.dev/dl/go1.23.6.linux-${ARCH}.tar.gz" | tar xz -C /usr/local && \
    /usr/local/go/bin/go install golang.org/x/tools/gopls@latest && \
    rm -rf /root/.cache/go-build
ENV PATH="${PATH}:/usr/local/go/bin:/root/go/bin"

# Rust-analyzer language server (standalone binary)
RUN ARCH=$(dpkg --print-architecture) && \
    if [ "$ARCH" = "amd64" ]; then RARCH="x86_64"; else RARCH="aarch64"; fi && \
    curl -sSL "https://github.com/rust-lang/rust-analyzer/releases/latest/download/rust-analyzer-${RARCH}-unknown-linux-gnu.gz" \
    | gunzip > /usr/local/bin/rust-analyzer && \
    chmod +x /usr/local/bin/rust-analyzer

# Marksman markdown language server (standalone binary)
RUN ARCH=$(dpkg --print-architecture) && \
    if [ "$ARCH" = "amd64" ]; then MARCH="linux-x64"; else MARCH="linux-arm64"; fi && \
    curl -sSL "https://github.com/artempyanykh/marksman/releases/latest/download/marksman-${MARCH}" \
    -o /usr/local/bin/marksman && \
    chmod +x /usr/local/bin/marksman

# OmniSharp C# language server
RUN ARCH=$(dpkg --print-architecture) && \
    if [ "$ARCH" = "amd64" ]; then OSARCH="linux-x64"; else OSARCH="linux-arm64"; fi && \
    mkdir -p /usr/local/bin/omnisharp && \
    curl -sSL "https://github.com/OmniSharp/omnisharp-roslyn/releases/latest/download/omnisharp-${OSARCH}-net6.0.tar.gz" \
    | tar xz -C /usr/local/bin/omnisharp && \
    chmod +x /usr/local/bin/omnisharp/OmniSharp

# Create a non-root user and the repos/data directories
# Add appuser to docker group for socket access
RUN groupadd -r appuser && useradd -r -g appuser -m appuser \
    && usermod -aG docker appuser \
    && mkdir -p /repos && chown appuser:appuser /repos \
    && mkdir -p /data && chown appuser:appuser /data

COPY --from=build /app/publish .
COPY entrypoint.sh /app/entrypoint.sh
RUN sed -i 's/\r$//' /app/entrypoint.sh && chmod +x /app/entrypoint.sh \
    && mkdir -p /app/wwwroot/uploads && chown -R appuser:appuser /app/wwwroot/uploads

# Configure credentials via environment variables at runtime:
#   docker run -e Git__Users__fennell=secret -e Git__RequireAuth=true ...
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ConnectionStrings__Default="Data Source=/data/mypersonalgit.db"
EXPOSE 8080 8443 2222

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

# Run as root so entrypoint can fix bind mount permissions, then drop to appuser
ENTRYPOINT ["/app/entrypoint.sh"]
