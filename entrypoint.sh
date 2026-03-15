#!/bin/bash
set -e

# Ensure data and repos directories exist and are writable by appuser
# This handles bind mounts created as root on the host
if [ "$(id -u)" = "0" ]; then
    chown -R appuser:appuser /data /repos 2>/dev/null || true
    # Allow appuser to access Docker socket for CI/CD workflow runner
    chmod 666 /var/run/docker.sock 2>/dev/null || true
    exec gosu appuser dotnet MyPersonalGit.dll "$@"
else
    exec dotnet MyPersonalGit.dll "$@"
fi
