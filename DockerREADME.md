# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](https://github.com/ChrisDFennell/MyPersonalGit) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/ChrisDFennell/MyPersonalGit/blob/main/LICENSE)

A self-hosted Git server with a GitHub-like web interface. Browse repositories, manage issues, pull requests, wikis, CI/CD, and more — all from your own machine.

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)

## Quick Start

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  fennch/mypersonalgit:latest
```

Then visit **http://localhost:8080** — default login: `admin` / `admin`

> Port 2222 is for the built-in SSH server (optional). Docker socket is for CI/CD (optional).

## Features

**Code & Repositories** — Full code browser, file editor, commit history, branches, tags, forks, compare view, language stats, branch protection, Git LFS, repository mirroring, import/migration, archiving

**Built-in SSH Server** — Native SSH for Git operations (Ed25519, RSA, ECDSA) — no external OpenSSH needed

**Issues & Pull Requests** — Labels, assignees, milestones, merge strategies (commit/squash/rebase), inline code reviews, suggestions, merge conflict resolution, draft PRs

**Discussions** — Threaded conversations with categories, pinning, locking, Q&A answers, upvoting

**CI/CD** — Auto-trigger workflows on push/PR, GitHub Actions compatibility, auto-release pipeline, commit status checks, concurrency controls, env vars, badges, artifact downloads, secrets

**Package & Container Hosting** — Docker/OCI registry, NuGet, npm, and generic package hosting

**Pages** — Serve static websites from repository branches

**Authentication** — OAuth2/SSO (8 providers), LDAP/Active Directory, TOTP 2FA, SSH keys, personal access tokens

**Web IDE** — Monaco Editor with LSP support (12 languages), AI code completion (OpenAI-compatible), Python debugger, problems panel, task runner, branch diff, inline diff gutters with click-to-view-original, Peek Definition, code snippets, colored file icons, unsaved close warnings, integrated terminal, Git integration, Zen mode

**Administration** — Database (SQLite/PostgreSQL), user management, audit logs, AI completion settings, customizable footer pages, dark mode

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `Database__Provider` | `sqlite` or `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | Database connection string | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Repository storage directory | `/repos` |
| `Git__RequireAuth` | Require auth for Git HTTP | `true` |
| `RESET_ADMIN_PASSWORD` | Emergency admin password reset | — |

SSH server, LDAP, and other settings are configured via the Admin dashboard.

## Docker Compose

```yaml
services:
  mypersonalgit:
    image: fennch/mypersonalgit:latest
    ports:
      - "8080:8080"
      - "2222:2222"
    volumes:
      - repos:/repos
      - data:/data
      - /var/run/docker.sock:/var/run/docker.sock

volumes:
  repos:
  data:
```

## PostgreSQL (Optional)

```yaml
services:
  mypersonalgit:
    image: fennch/mypersonalgit:latest
    ports:
      - "8080:8080"
    environment:
      - Database__Provider=postgresql
      - ConnectionStrings__Default=Host=db;Database=mypersonalgit;Username=mypg;Password=secret
    depends_on:
      - db
    volumes:
      - repos:/repos

  db:
    image: postgres:17
    environment:
      - POSTGRES_DB=mypersonalgit
      - POSTGRES_USER=mypg
      - POSTGRES_PASSWORD=secret
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  repos:
  pgdata:
```

## NAS Deployment

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  fennch/mypersonalgit:latest
```

## Full Documentation

For complete setup guides, SSH configuration, LDAP/AD setup, CI/CD workflows, package registries, and more:

**[Full README on GitHub](https://github.com/ChrisDFennell/MyPersonalGit)**

## License

MIT
