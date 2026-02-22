# MyPersonalGit

A self-hosted Git server with a GitHub-like web interface built with ASP.NET Core and Blazor Server. Browse repositories, manage issues, pull requests, wikis, projects, and more — all from your own machine or server.

![MyPersonalGit Screenshot](assets/images/screenshot.png)

## Features

- **Repository Management** — Create, browse, and delete Git repositories with a full code browser, file editor, commit history, branches, and tags
- **Issues & Pull Requests** — Create, comment on, close/reopen issues and PRs with labels, assignees, and reviews. Merge PRs with merge commit, squash, or rebase strategies
- **Dark Mode** — Full dark mode support with a toggle in the header, saved to localStorage
- **Activity Feed** — See recent activity across all repositories on the home page
- **Compare View** — Compare branches with ahead/behind commit counts and full diff rendering
- **Language Stats** — GitHub-style language breakdown bar on each repository page
- **CI/CD Runner** — Define workflows in `.github/workflows/*.yml` and run them in Docker containers
- **Wiki** — Markdown-based wiki pages per repository with revision history
- **Projects** — Kanban boards with drag-and-drop cards for organizing work
- **Security** — Security advisories, dependency scanning, and vulnerability tracking
- **User Profiles** — Contribution heatmap, activity feed, and stats per user
- **Branch Protection** — Configurable rules for required reviews, status checks, and force-push prevention
- **Git Smart HTTP** — Clone, fetch, and push over HTTP with Basic Auth
- **Search** — Full-text search across repositories, issues, PRs, and code
- **Notifications** — In-app notifications for mentions, comments, and repository activity
- **Admin Dashboard** — System settings, user management, audit logs, and statistics

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | ASP.NET Core 8.0 |
| Frontend | Blazor Server (interactive server-side rendering) |
| Database | SQLite via Entity Framework Core 8 |
| Git Engine | LibGit2Sharp |
| Auth | BCrypt password hashing, session-based authentication |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |

## Quick Start

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) (recommended)
- Or [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) + Git for local development

### Docker (Recommended)

Pull from Docker Hub and run:

```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

Or use Docker Compose:

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit
docker compose up -d
```

The app will be available at **http://localhost:8080**.

> **Default credentials**: `admin` / `admin`
>
> **Change the default password immediately** via the Admin dashboard after first login.

### Run Locally

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

The app starts at **http://localhost:5146**.

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__Default` | SQLite connection string | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Directory where Git repos are stored | `/repos` |
| `Git__RequireAuth` | Require auth for Git HTTP operations | `true` |
| `Git__Users__<username>` | Set password for Git HTTP Basic Auth user | — |

## Usage

### 1. Sign In

Open the app and click **Sign In**. On a fresh install, use the default credentials (`admin` / `admin`). Create additional users via the **Admin** dashboard or by enabling user registration in Admin > Settings.

### 2. Create a Repository

Click the green **New** button on the home page, enter a name, and click **Create**. This creates a bare Git repository on the server that you can clone, push to, and manage through the web UI.

### 3. Clone and Push

```bash
git clone http://localhost:8080/git/MyRepo.git
cd MyRepo

echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

If Git HTTP auth is enabled, you'll be prompted for the credentials configured via `Git__Users__<username>` environment variables. These are separate from the web UI login.

### 4. Clone from an IDE

**VS Code**: `Ctrl+Shift+P` > **Git: Clone** > paste `http://localhost:8080/git/MyRepo.git`

**Visual Studio**: **Git > Clone Repository** > paste the URL

**JetBrains**: **File > New > Project from Version Control** > paste the URL

### 5. Use the Web Editor

You can edit files directly in the browser:
- Navigate to a repository and click on any file, then click **Edit**
- Use **Add files > Create new file** to add files without a local clone
- Use **Add files > Upload files/folder** to upload from your machine

## Deploy to a NAS

MyPersonalGit works great on a NAS (QNAP, Synology, etc.) via Docker:

```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

The Docker socket mount is optional — only needed if you want CI/CD workflow execution.

## Configuration

All settings can be configured in `appsettings.json`, via environment variables, or through the Admin dashboard at `/admin`:

- Project root directory
- Authentication requirements
- User registration settings
- Feature toggles (Issues, Wiki, Projects, Actions)
- Max repository size and count per user
- SMTP settings for email notifications

## Project Structure

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Blazor pages (Home, RepoDetails, Issues, PRs, etc.)
  Controllers/       # REST API endpoints
  Data/              # EF Core DbContext, service implementations
  Models/            # Domain models
  Migrations/        # EF Core migrations
  Services/          # Middleware (auth, Git HTTP backend)
  Program.cs         # App startup, DI, middleware pipeline
MyPersonalGit.Tests/
  UnitTest1.cs       # xUnit tests with InMemory database
```

## Running Tests

```bash
dotnet test
```

## License

MIT
