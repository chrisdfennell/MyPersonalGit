# MyPersonalGit

A self-hosted Git server with a GitHub-like web interface built with ASP.NET Core and Blazor Server. Browse repositories, manage issues, pull requests, wikis, projects, and more — all from your own machine or server.

![MyPersonalGit Screenshot](assets/images/screenshot.png)

## Features

### Code & Repositories
- **Repository Management** — Create, browse, and delete Git repositories with a full code browser, file editor, commit history, branches, and tags
- **Repository Archiving** — Mark repositories as read-only with visual badges; pushes are blocked for archived repos
- **Git Smart HTTP** — Clone, fetch, and push over HTTP with Basic Auth
- **Git LFS** — Large File Storage support for tracking binary files
- **Repository Mirroring** — Mirror repositories to/from external Git remotes
- **Compare View** — Compare branches with ahead/behind commit counts and full diff rendering
- **Language Stats** — GitHub-style language breakdown bar on each repository page
- **Branch Protection** — Configurable rules for required reviews, status checks, and force-push prevention
- **Search** — Full-text search across repositories, issues, PRs, and code

### Collaboration
- **Issues & Pull Requests** — Create, comment on, close/reopen issues and PRs with labels, assignees, and reviews. Merge PRs with merge commit, squash, or rebase strategies
- **Wiki** — Markdown-based wiki pages per repository with revision history
- **Projects** — Kanban boards with drag-and-drop cards for organizing work
- **Snippets** — Share code snippets (like GitHub Gists) with syntax highlighting and multiple files

### CI/CD & DevOps
- **CI/CD Runner** — Define workflows in `.github/workflows/*.yml` and run them in Docker containers
- **Webhooks** — Trigger external services on repository events
- **Prometheus Metrics** — Built-in `/metrics` endpoint for monitoring

### Package & Container Hosting
- **Container Registry** — Host Docker/OCI images with `docker push` and `docker pull` (OCI Distribution Spec)
- **NuGet Registry** — Host .NET packages with full NuGet v3 API (service index, search, push, restore)
- **npm Registry** — Host Node.js packages with standard npm publish/install
- **Generic Packages** — Upload and download arbitrary binary artifacts via REST API

### Static Sites
- **Pages** — Serve static websites directly from a repository branch (like GitHub Pages) at `/pages/{owner}/{repo}/`

### Notifications
- **In-App Notifications** — Mentions, comments, and repository activity
- **Push Notifications** — Ntfy and Gotify integration for real-time mobile/desktop alerts with per-user opt-in

### Administration
- **Admin Dashboard** — System settings, user management, audit logs, and statistics
- **User Profiles** — Contribution heatmap, activity feed, and stats per user
- **Personal Access Tokens** — Token-based API authentication with configurable scopes
- **Backup & Restore** — Export and import server data
- **Security** — Security advisories, dependency scanning, and vulnerability tracking
- **Dark Mode** — Full dark/light mode support with a toggle in the header

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | ASP.NET Core 8.0 |
| Frontend | Blazor Server (interactive server-side rendering) |
| Database | SQLite via Entity Framework Core 8 |
| Git Engine | LibGit2Sharp |
| Auth | BCrypt password hashing, session-based authentication, PAT tokens |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| Monitoring | Prometheus metrics |

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
| `RESET_ADMIN_PASSWORD` | Emergency admin password reset on startup | — |

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

### 6. Container Registry

Push and pull Docker/OCI images directly to your server:

```bash
# Log in (use a Personal Access Token from Settings > Access Tokens)
docker login localhost:8080 -u youruser

# Push an image
docker tag myapp:latest localhost:8080/myapp:v1
docker push localhost:8080/myapp:v1

# Pull an image
docker pull localhost:8080/myapp:v1
```

> **Note:** Docker requires HTTPS by default. For HTTP, add your server to Docker's `insecure-registries` in `~/.docker/daemon.json`:
> ```json
> { "insecure-registries": ["localhost:8080"] }
> ```

### 7. Package Registry

**NuGet (.NET packages):**
```bash
dotnet nuget add source http://localhost:8080/api/packages/nuget/v3/index.json \
  --name mygit --username youruser --password yourPAT
dotnet nuget push MyPackage.1.0.0.nupkg --source mygit --api-key yourPAT
```

**npm (Node.js packages):**
```bash
npm config set //localhost:8080/api/packages/npm/:_authToken="yourPAT"
npm publish --registry=http://localhost:8080/api/packages/npm
```

**Generic (any binary):**
```bash
curl -u youruser:yourPAT -X PUT \
  --upload-file myfile.zip \
  http://localhost:8080/api/packages/generic/my-tool/1.0.0/myfile.zip
```

Browse all packages at `/packages` in the web UI.

### 8. Pages (Static Site Hosting)

Serve static websites from a repository branch:

1. Go to your repository's **Settings** tab and enable **Pages**
2. Set the branch (default: `gh-pages`)
3. Push HTML/CSS/JS to that branch
4. Visit `http://localhost:8080/pages/{username}/{repo}/`

### 9. Push Notifications

Configure Ntfy or Gotify in **Admin > System Settings** to receive push notifications on your phone or desktop when issues, PRs, or comments are created. Users can opt in/out in **Settings > Notifications**.

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
- Push notification settings (Ntfy/Gotify)

## Project Structure

```
MyPersonalGit/
  Components/
    Layout/          # MainLayout, NavMenu
    Pages/           # Blazor pages (Home, RepoDetails, Issues, PRs, Packages, etc.)
  Controllers/       # REST API endpoints (NuGet, npm, Generic, Registry, etc.)
  Data/              # EF Core DbContext, service implementations
  Models/            # Domain models
  Migrations/        # EF Core migrations
  Services/          # Middleware (auth, Git HTTP backend, Pages, Registry auth)
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
