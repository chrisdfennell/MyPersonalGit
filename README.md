# MyPersonalGit

A self-hosted Git server with a GitHub-like web interface. Browse repositories, manage issues, pull requests, wikis, projects, and more — all from your own machine or server.

## Features

- **Repository Management** — Create, browse, and delete Git repositories with a full code browser, file editor, commit history, branches, and tags
- **Issues & Pull Requests** — Create, comment on, close/reopen issues and PRs with labels, assignees, and reviews
- **Wiki** — Markdown-based wiki pages per repository with revision history
- **Projects** — Kanban boards with drag-and-drop cards for organizing work
- **Actions/CI-CD** — Workflow runs and webhook management
- **Security** — Security advisories, dependency scanning, and vulnerability tracking
- **User Management** — Registration, login, admin roles, SSH keys, personal access tokens, 2FA
- **Branch Protection** — Configurable rules for required reviews, status checks, and force-push prevention
- **Git Smart HTTP** — Clone, fetch, and push over HTTP with Basic Auth
- **Search** — Full-text search across repositories, issues, PRs, and code
- **Notifications** — In-app notifications for mentions, comments, and repository activity
- **Admin Dashboard** — System settings, user management, audit logs, and statistics

## Tech Stack

- **Backend**: ASP.NET Core 8.0
- **Frontend**: Blazor Server (interactive server-side rendering)
- **Database**: SQLite via Entity Framework Core 8
- **Git Engine**: LibGit2Sharp
- **Auth**: BCrypt password hashing, session-based authentication
- **Markdown**: Markdig

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Git (required for `git http-backend` Smart HTTP support)

### Run Locally

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

The app starts at **http://localhost:5146**. On first run, the database is created automatically and a default admin account is seeded.

> **Default credentials**: `admin` / `admin`
>
> **Change the default password immediately** via the Admin dashboard after first login.

### Docker

```bash
docker build -t mypersonalgit ./MyPersonalGit
docker run -d -p 8080:8080 -v mypersonalgit-data:/app -v mypersonalgit-repos:/repos mypersonalgit
```

The app will be available at **http://localhost:8080**.

#### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__Default` | SQLite connection string | `Data Source=mypersonalgit.db` |
| `Git__ProjectRoot` | Directory where Git repos are stored | `/repos` |
| `Git__RequireAuth` | Require auth for Git HTTP operations | `true` |
| `Git__Users__<username>` | Set password for Git HTTP Basic Auth user | — |

Example with Git auth:
```bash
docker run -d -p 8080:8080 \
  -e Git__Users__fennell=mysecretpassword \
  -e Git__RequireAuth=true \
  -v mypersonalgit-data:/app \
  -v mypersonalgit-repos:/repos \
  mypersonalgit
```

## Usage

### 1. Sign In

Open **http://localhost:5146** and click **Sign in**. On a fresh install, use the default credentials (`admin` / `admin`). You can create additional users via the **Admin** dashboard or by enabling user registration in Admin > Settings.

### 2. Create a Repository

Click the green **New** button on the home page, enter a name, and click **Create**. This creates a bare Git repository on the server that you can clone, push to, and manage through the web UI.

### 3. Clone and Push from the Command Line

```bash
# Clone the empty repo
git clone http://localhost:5146/git/MyRepo.git
cd MyRepo

# Add some code and push
echo "# My Project" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

If Git HTTP auth is enabled (`Git:RequireAuth = true`), you'll be prompted for the credentials configured in `Git:Users` (these are separate from the web UI login — see [Configuration](#configuration)).

### 4. Clone and Push from Visual Studio Code

1. Open VS Code and press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
2. Type **Git: Clone** and press Enter
3. Paste the clone URL: `http://localhost:5146/git/MyRepo.git`
4. Choose a local folder and open the cloned repo
5. Make changes, commit with the Source Control panel (`Ctrl+Shift+G`), and click **Sync Changes** to push

### 5. Clone and Push from Visual Studio

1. Open Visual Studio and go to **Git > Clone Repository**
2. Paste the clone URL: `http://localhost:5146/git/MyRepo.git`
3. Choose a local path and click **Clone**
4. Make changes, then use **Git > Commit or Stash** to commit
5. Click **Push** in the Git toolbar to push to MyPersonalGit

### 6. Clone and Push from JetBrains IDEs (Rider, IntelliJ, etc.)

1. Go to **File > New > Project from Version Control**
2. Paste the clone URL: `http://localhost:5146/git/MyRepo.git`
3. Click **Clone**
4. Commit and push using **Git > Push** (`Ctrl+Shift+K`)

### 7. Use the Web Editor

You can also edit files directly in the browser:
- Navigate to a repository and click on any file
- Click **Edit** to modify it inline
- Enter a commit message and click **Commit changes**
- Use **Add files > Create new file** to add files without a local clone
- Use **Add files > Upload files/folder** to upload from your machine

### 8. Manage Issues, PRs, and More

Each repository has tabs for:
- **Issues** — Bug reports, feature requests, and discussions
- **Pull Requests** — Code review and merge workflow
- **Wiki** — Documentation pages with Markdown and revision history
- **Projects** — Kanban boards for organizing work
- **Actions** — Workflow runs and webhooks
- **Security** — Advisories and vulnerability scanning
- **Insights** — Repository statistics

## Configuration

All settings can be configured in `appsettings.json` or via the Admin dashboard:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=mypersonalgit.db"
  },
  "Git": {
    "ProjectRoot": "/repos",
    "RequireAuth": true
  }
}
```

The Admin dashboard (accessible to admin users at `/admin`) allows you to configure:
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
