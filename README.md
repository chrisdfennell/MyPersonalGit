🌐 **Language / Idioma / Langue:** [English](README.md) | [Español](README.es.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [中文](README.zh.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [Italiano](README.it.md) | [Türkçe](README.tr.md)

# MyPersonalGit

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/) [![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) [![SQLite](https://img.shields.io/badge/SQLite-Default-003B57?logo=sqlite&logoColor=white)](https://www.sqlite.org/) [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Optional-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/) [![Docker](https://img.shields.io/badge/Docker-Hub-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/fennch/mypersonalgit) [![CI/CD](https://img.shields.io/badge/CI%2FCD-Auto_Release-brightgreen?logo=githubactions&logoColor=white)](#ci-cd) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![GitHub last commit](https://img.shields.io/github/last-commit/ChrisDFennell/MyPersonalGit)](https://github.com/ChrisDFennell/MyPersonalGit)

A self-hosted Git server with a GitHub-like web interface built with ASP.NET Core and Blazor Server. Browse repositories, manage issues, pull requests, wikis, projects, and more — all from your own machine or server.

![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot2.png)
![MyPersonalGit Screenshot](https://github.com/chrisdfennell/MyPersonalGit/raw/main/assets/images/screenshot3.png)

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Quick Start](#quick-start)
  - [Docker (Recommended)](#docker-recommended)
  - [Run Locally](#run-locally)
  - [Environment Variables](#environment-variables)
- [Usage](#usage)
  - [Sign In](#1-sign-in)
  - [Create a Repository](#2-create-a-repository)
  - [Clone and Push](#3-clone-and-push)
  - [Clone from an IDE](#4-clone-from-an-ide)
  - [Web IDE](#web-ide)
  - [Container Registry](#6-container-registry)
  - [Package Registry](#7-package-registry)
  - [Pages (Static Sites)](#8-pages-static-site-hosting)
  - [Push Notifications](#9-push-notifications)
  - [SSH Key Authentication](#10-ssh-key-authentication)
  - [LDAP / Active Directory](#11-ldap--active-directory-authentication)
  - [Repository Secrets](#12-repository-secrets)
  - [OAuth / SSO Login](#13-oauth--sso-login)
  - [Import Repository](#14-import-repository)
  - [Forking & Upstream Sync](#15-forking--upstream-sync)
  - [CI/CD Auto-Release](#16-cicd-auto-release)
  - [RSS/Atom Feeds](#17-rssatom-feeds)
- [Database Configuration](#database-configuration)
  - [Using PostgreSQL](#using-postgresql)
  - [Switching from the Admin Dashboard](#switching-from-the-admin-dashboard)
  - [Choosing a Database](#choosing-a-database)
- [Deploy to a NAS](#deploy-to-a-nas)
- [Configuration](#configuration)
- [Project Structure](#project-structure)
- [Running Tests](#running-tests)
- [License](#license)

---

## Features

### Code & Repositories
- **Repository Management** — Create, browse, and delete Git repositories with a full code browser, file editor, commit history, branches, and tags
- **Repository Import/Migration** — Import repositories from GitHub, GitLab, Bitbucket, Gitea/Forgejo/Gogs, or any Git URL with optional issue and PR import. Background processing with progress tracking
- **Repository Archiving** — Mark repositories as read-only with visual badges; pushes are blocked for archived repos
- **Git Smart HTTP** — Clone, fetch, and push over HTTP with Basic Auth
- **Built-in SSH Server** — Native SSH server for Git operations — no external OpenSSH required. Supports ECDH key exchange, AES-CTR encryption, and public key authentication (RSA, ECDSA, Ed25519)
- **SSH Key Authentication** — Add SSH public keys to your account and authenticate Git operations via SSH with auto-managed `authorized_keys` (or the built-in SSH server)
- **Forks & Upstream Sync** — Fork repositories, sync forks with upstream with one click, and see fork relationships in the UI
- **Git LFS** — Large File Storage support for tracking binary files
- **Repository Mirroring** — Mirror repositories to/from external Git remotes
- **Compare View** — Compare branches with ahead/behind commit counts and full diff rendering
- **Language Stats** — GitHub-style language breakdown bar on each repository page
- **Branch Protection** — Configurable rules for required reviews, status checks, force-push prevention, required linear history (rebase-only), and CODEOWNERS approval enforcement
- **Signed Commits Required** — Branch protection rule to require all commits be GPG-signed before merging
- **Tag Protection** — Protect tags from deletion, force updates, and unauthorized creation with glob pattern matching and per-user allow lists
- **Commit Signature Verification** — GPG signature verification on commits and annotated tags with "Verified" / "Signed" badges in the UI
- **Repository Labels** — Manage labels with custom colors per repository; labels are automatically copied when creating repos from templates
- **AGit Flow** — Push-to-review workflow: `git push origin HEAD:refs/for/main` creates a pull request without forking or creating remote branches. Updates existing open PRs on subsequent pushes
- **Explore** — Browse all accessible repositories with search, sort, and topic filtering
- **Star from Explore** — Star and unstar repositories directly from the Explore page without opening each repo
- **Autolink References** — Auto-convert `#123` to issue links, plus configurable custom patterns (e.g., `JIRA-456` → external URLs) per repository
- **Search** — Full-text search across repositories, issues, PRs, and code
- **Blame View** — Line-by-line authorship tracking with commit details for every line in a file
- **License Detection** — Automatically detects LICENSE files and identifies common licenses (MIT, Apache-2.0, GPL, BSD, ISC, MPL, Unlicense) with a badge in the repository sidebar

### Collaboration
- **Issues & Pull Requests** — Create, comment on, close/reopen issues and PRs with labels, multiple assignees, due dates, and reviews. Merge PRs with merge commit, squash, or rebase strategies. Web-based merge conflict resolution with side-by-side diff view
- **Issue Dependencies** — Define "blocked by" and "blocks" relationships between issues with circular dependency detection
- **Issue Pinning & Locking** — Pin important issues to the top of the list and lock conversations to prevent further comments
- **Comment Editing & Deletion** — Edit or delete your own comments on issues and pull requests with "(edited)" indicator
- **@Mention Notifications** — @mention users in comments to send them a direct notification
- **Merge Conflict Resolution** — Resolve merge conflicts directly in the browser with a visual editor showing base/ours/theirs views, quick accept buttons, and conflict marker validation
- **Squash Commit Message** — Customize the commit message when squash-merging a pull request
- **Branch Delete After Merge** — Option to automatically delete the source branch after merging a pull request, enabled by default
- **Discussions** — GitHub Discussions-style threaded conversations per repository with categories (General, Q&A, Announcements, Ideas, Show & Tell, Polls), pin/lock, mark as answer, and upvoting
- **Code Review Suggestions** — "Suggest changes" mode in PR inline reviews lets reviewers propose code replacements directly in the diff
- **Image Diff** — Side-by-side image comparison in pull requests with opacity slider for visual diffing of changed images (PNG, JPG, GIF, SVG, WebP)
- **File Tree in PRs** — Collapsible file tree sidebar in pull request diff view for easy navigation between changed files
- **Mark Files as Viewed** — Track review progress in pull requests with per-file "Viewed" checkboxes and a progress counter
- **Diff Syntax Highlighting** — Language-aware syntax coloring in pull request and compare diffs via Prism.js
- **Reaction Emoji** — React to issues, PRs, discussions, and comments with thumbs up/down, heart, laugh, hooray, confused, rocket, and eyes
- **Auto-Merge** — Enable auto-merge on pull requests to automatically merge when all required status checks pass and reviews are approved
- **CI Status on PR List** — Pull request list shows green/red/yellow CI status icons next to each PR title
- **Cherry-Pick / Revert via UI** — Cherry-pick any commit to another branch or revert a commit, either directly or as a new pull request, from the web interface
- **Transfer Issues** — Move issues between repositories, preserving title, body, comments, matching labels, and linking the original with a transfer note
- **Saved Replies** — Save canned responses and quickly insert them when commenting on issues or pull requests
- **Batch Issue Operations** — Select multiple issues and close or reopen them in bulk from the issue list
- **CODEOWNERS** — Auto-assign PR reviewers based on file paths with optional enforcement requiring CODEOWNERS approval before merge
- **Repository Templates** — Create new repositories from templates with automatic copying of files, labels, issue templates, and branch protection rules
- **Draft Issues & Issue Templates** — Create draft issues (work-in-progress) and define reusable issue templates (bug report, feature request) per repository with default labels
- **PR Template** — Automatically pre-fill pull request descriptions from `.github/PULL_REQUEST_TEMPLATE.md`
- **Release Editing** — Edit release titles, descriptions, and draft/pre-release flags after creation
- **Wiki** — Markdown-based wiki pages per repository with revision history
- **Projects** — Kanban boards with drag-and-drop cards for organizing work
- **Snippets** — Share code snippets (like GitHub Gists) with syntax highlighting and multiple files
- **Organizations & Teams** — Create organizations with members and teams, assign team permissions to repositories
- **Granular Permissions** — Five-tier permission model (Read, Triage, Write, Maintain, Admin) for fine-grained access control on repositories
- **Milestones** — Track issue progress toward milestones with progress bars and due dates
- **Time Tracking** — Log time entries on issues with start/stop timers and manual duration entry
- **Commit Comments** — Comment on individual commits with optional file/line references
- **Repository Topics** — Tag repositories with topics for discovery and filtering on the Explore page
- **Activity Pulse** — Weekly summary page per repository showing PRs merged, issues opened/closed, commits, top contributors, and active branches over the last 7 days

### Web IDE
- **Full-Featured Code Editor** — Monaco Editor-powered IDE with 30+ language syntax highlighting, multiple themes, Emmet abbreviation expansion for HTML/CSS, and format-on-paste
- **Language Server Protocol (LSP)** — IntelliSense, autocomplete, hover info, go-to-definition, find references, signature help, code formatting, rename symbol, code actions, and real-time diagnostics for 12 languages: C# (OmniSharp), TypeScript/JavaScript, Python, Go, Rust, HTML, CSS, JSON, YAML, Bash, Dockerfile, and Markdown
- **AI Code Completion** — Inline ghost-text suggestions as you type, powered by any OpenAI-compatible API (OpenAI, Ollama, LM Studio, etc.). Configurable endpoint, API key, and model in admin settings. Tab to accept
- **AI Chat & Assistant** — Integrated AI chat panel with context-aware conversations. Right-click code for AI actions: Explain Code, Refactor Code, Generate Tests, Fix Code, Add Documentation, and Inline Explain (Ctrl+Shift+E shows explanation widget at cursor). Streaming responses with code block apply/copy/new-file actions
- **AI Test Generation** — Select code and generate comprehensive unit tests with framework auto-detection (xUnit, Jest, pytest, Go testing, Rust #[cfg(test)], JUnit 5, RSpec, PHPUnit)
- **AI-Assisted Refactoring** — Select code and get AI-powered refactoring suggestions with one-click apply to replace the selection
- **Integrated Debugger** — Multi-language debugging via DAP (Debug Adapter Protocol) with breakpoints (click gutter), step over/into/out, continue, variable inspection, call stack navigation, and debug console output. Supports Python (debugpy), C# (netcoredbg), JavaScript/TypeScript (js-debug), Go (Delve), Rust (lldb-dap), and Java (JDWP). Auto-detects language from file extension
- **Problems Panel** — Bottom panel tab aggregating all LSP diagnostics (errors/warnings) across open files, grouped by file with click-to-navigate. Error/warning counts in the status bar
- **Task Runner** — Bottom panel tab to run build/test commands (dotnet build, npm test, go build, make, pytest, etc.) with real-time streamed output, clickable error locations, and cancel support
- **Branch Diff** — Sidebar panel to compare two branches side-by-side. Shows list of changed files with add/modify/delete/rename status, click to view diff in Monaco's diff editor
- **Inline Diff Gutters** — Green, blue, and red indicators in the editor gutter showing added, modified, and deleted lines vs the last commit. Click a gutter bar to see the original content inline
- **Code Snippets** — User-defined code snippets stored per-browser. Define prefix, language, and body in Settings; type the prefix and Tab to expand. Supports VS Code snippet syntax ($1, $2, $0 placeholders)
- **File Management** — Hierarchical file tree with language-specific colored icons (40+ file types), file nesting (groups `.razor` + `.razor.css` + `.razor.cs`), search/filter, drag-and-drop file upload, and context menus for new file/folder/rename/delete
- **Tab Management** — Multi-tab interface with colored file icons, drag-to-reorder, pinned tabs, unsaved changes close confirmation, undo close tab (Ctrl+Shift+T), right-click context menu (Close, Close Others, Close to the Right, Close Saved), and scroll chevrons for overflow
- **Split Editor & Diff View** — Side-by-side editing with independent scroll, and diff view for comparing changes before commit
- **Integrated Terminal** — xterm.js-based terminal with multiple terminal tabs, WebSocket shell access, and theme-aware light/dark mode
- **Git Integration** — Branch creation, blame view, file history, commit panel, source control with file selection, and a visual commit graph with colored lane lines and branch labels
- **Merge Conflict Resolution** — Inline Accept Current / Accept Incoming / Accept Both buttons with color-coded conflict regions (green/blue)
- **Search & Replace** — Global search across all files with file extension filtering, line-by-line results, and replace all
- **Code Navigation** — Quick Open (Ctrl+P), Command Palette (Ctrl+Shift+P), Go to Line (Ctrl+G), Go to Definition (F12), Peek Definition (Alt+F12), Find References (Shift+F12), Rename Symbol (F2), outline/symbols panel, and breadcrumb navigation
- **Keyboard Shortcuts** — VS Code-style shortcuts (Ctrl+Shift+P, Ctrl+P, Ctrl+Shift+F, Ctrl+Shift+M, Ctrl+Shift+T, Ctrl+`, F5, F9, Escape) intercepted at document level to prevent browser defaults
- **CSS Color Previews** — Inline color swatches next to hex/rgb/hsl values in CSS, SCSS, and Less files
- **Minimap Highlights** — Modified lines, added lines, and conflict markers shown as colored markers in the minimap gutter
- **Markdown & Image Preview** — Toggle between edit and rendered preview for Markdown files; inline image display for common formats
- **Auto-Save** — Optional auto-commit with configurable delay (500ms–5s), toggleable in settings or command palette
- **Persistent Workspace** — Remembers open tabs, pinned tabs, active file, sidebar state, and panel mode across browser sessions
- **Resizable Panels** — Drag-to-resize sidebar and bottom panel with visual handles
- **Customizable Settings** — Font family picker (8 fonts), font size, tab size, word wrap, minimap, line numbers, bracket guides, sticky scroll, code folding, font ligatures, format on paste, and render whitespace options
- **Real-time Collaboration** — Multiple users can edit the same file simultaneously with live cursor tracking, selection highlighting, and presence indicators. SignalR-powered with operational transform for conflict-free concurrent edits. Color-coded cursors with username labels and automatic reconnection
- **Zen Mode** — Distraction-free full-screen editing with visible exit button

### CI/CD & DevOps
- **CI/CD Runner** — Define workflows in `.github/workflows/*.yml` and run them in Docker containers. Auto-triggers on push and pull request events
- **GitHub Actions Compatibility** — Same workflow YAML works on both MyPersonalGit and GitHub Actions. Translates `uses:` actions (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`, `actions/setup-python`, `actions/setup-java`, `docker/login-action`, `docker/build-push-action`, `softprops/action-gh-release`) into equivalent shell commands
- **Parallel Jobs with `needs:`** — Jobs declare dependencies via `needs:` and run in parallel when independent. Dependent jobs wait for their prerequisites and are automatically cancelled if a dependency fails
- **Conditional Steps (`if:`)** — Steps support `if:` expressions: `always()`, `success()`, `failure()`, `cancelled()`, `true`, `false`. Cleanup steps with `if: failure()` or `if: always()` still run after earlier failures
- **Step Outputs (`$GITHUB_OUTPUT`)** — Steps can write `key=value` or `key<<DELIMITER` multiline pairs to `$GITHUB_OUTPUT` and subsequent steps receive them as environment variables, compatible with `${{ steps.X.outputs.Y }}` syntax
- **`github` Context** — `GITHUB_SHA`, `GITHUB_REF`, `GITHUB_REF_NAME`, `GITHUB_ACTOR`, `GITHUB_REPOSITORY`, `GITHUB_EVENT_NAME`, `GITHUB_WORKSPACE`, `GITHUB_RUN_ID`, `GITHUB_JOB`, `GITHUB_WORKFLOW`, and `CI=true` automatically injected into every job
- **Matrix Builds** — `strategy.matrix` expands jobs across multiple variable combinations (e.g., OS x version). Supports `fail-fast` and `${{ matrix.X }}` substitution in `runs-on`, step commands, and step names
- **`workflow_dispatch` Inputs** — Manual triggers with typed input parameters (string, boolean, choice, number). UI shows an input form when triggering workflows with inputs. Values injected as `INPUT_*` env vars
- **Job Timeouts (`timeout-minutes`)** — Set `timeout-minutes` on jobs to automatically fail them if they exceed the limit. Default: 360 minutes (matches GitHub Actions)
- **Job-Level `if:`** — Skip entire jobs based on conditions. Jobs with `if: always()` run even when dependencies fail. Skipped jobs don't fail the run
- **Job Outputs** — Jobs declare `outputs:` that downstream `needs:` jobs consume via `${{ needs.X.outputs.Y }}`. Outputs are resolved from step outputs after the job completes
- **`continue-on-error`** — Mark individual steps as allowed to fail without failing the job. Useful for optional validation or notification steps
- **`on.push.paths` Filter** — Only trigger workflows when specific files change. Supports glob patterns (`src/**`, `*.ts`) and `paths-ignore:` for exclusions
- **Re-run Workflows** — Re-run failed, succeeded, or cancelled workflow runs with one click from the Actions UI. Creates a fresh run with the same configuration
- **`working-directory`** — Set `defaults.run.working-directory` at workflow level or per-step `working-directory:` to control where commands execute
- **`defaults.run.shell`** — Configure custom shell per workflow or per step (`bash`, `sh`, `python3`, etc.)
- **`strategy.max-parallel`** — Limit concurrent matrix job execution
- **Reusable Workflows (`workflow_call`)** — Define workflows with `on: workflow_call` that other workflows can invoke with `uses: ./.github/workflows/build.yml`. Supports typed inputs, outputs, and secrets. Called workflow jobs are inlined into the caller
- **Composite Actions** — Define multi-step actions in `.github/actions/{name}/action.yml` with `runs: using: composite`. Steps from composite actions are expanded inline during execution
- **Environment Deployments** — Configure deployment environments (e.g., `staging`, `production`) with protection rules: required reviewers, wait timers, and branch restrictions. Workflow jobs with `environment:` gate on approval before executing. Full deployment history with approve/reject UI
- **`on.workflow_run`** — Chain workflows: trigger workflow B when workflow A completes. Filter by workflow name and `types: [completed]`
- **Automatic Release Creation** — `softprops/action-gh-release` creates real Release entities with tag, title, changelog body, and pre-release/draft flags. Source code archives (ZIP and TAR.GZ) are automatically attached as downloadable assets
- **Auto-Release Pipeline** — Built-in workflow auto-tags versions, generates changelogs, and pushes Docker images to Docker Hub on every push to main
- **Commit Status Checks** — Workflows automatically set pending/success/failure status on commits, visible on pull requests
- **Workflow Cancellation** — Cancel running or queued workflows from the Actions UI
- **Concurrency Controls** — New pushes automatically cancel queued runs of the same workflow
- **Workflow Environment Variables** — Set `env:` at workflow, job, or step level in YAML
- **Status Badges** — Embeddable SVG badges for workflow and commit status (`/api/badge/{repo}/workflow`)
- **Artifact Downloads** — Download build artifacts directly from the Actions UI
- **Secrets Management** — Encrypted repository secrets (AES-256) injected as environment variables into CI/CD workflow runs
- **Webhooks** — Trigger external services on repository events
- **Prometheus Metrics** — Built-in `/metrics` endpoint for monitoring

### Package & Container Hosting (20 registries)
- **Container Registry** — Host Docker/OCI images with `docker push` and `docker pull` (OCI Distribution Spec)
- **NuGet Registry** — Host .NET packages with full NuGet v3 API (service index, search, push, restore)
- **npm Registry** — Host Node.js packages with standard npm publish/install
- **PyPI Registry** — Host Python packages with PEP 503 Simple API, JSON metadata API, and `twine upload` compatibility
- **Maven Registry** — Host Java/JVM packages with standard Maven repository layout, `maven-metadata.xml` generation, and `mvn deploy` support
- **Cargo Registry** — Host Rust crates with Cargo publish format, crate metadata API, and download endpoints
- **RubyGems Registry** — Host Ruby gems with `gem push`/`gem install`, dependency API, and gem metadata
- **Composer Registry** — Host PHP packages with Composer v2 API (`packages.json`, `p2/` metadata)
- **Helm Registry** — Host Kubernetes Helm charts with `helm push`/`helm install` and dynamic `index.yaml` generation
- **Conda Registry** — Host Conda packages with channel-based `repodata.json` and `.tar.bz2`/`.conda` support
- **Conan Registry** — Host C/C++ Conan packages with recipe upload/download and `conan remote add` support
- **Vagrant Registry** — Host Vagrant boxes with Vagrant Cloud-compatible metadata API
- **Pub Registry** — Host Dart/Flutter packages with pub.dev-compatible API and `pubspec.yaml` parsing
- **Swift Registry** — Host Swift packages with Swift Package Manager registry API and `Package.swift` manifest serving
- **Debian Registry** — Host `.deb` packages with APT-compatible `Packages` index and `Release` file generation
- **CRAN Registry** — Host R packages with `PACKAGES` index generation for `install.packages()`
- **Alpine Registry** — Host Alpine Linux `.apk` packages with APKINDEX generation
- **RPM Registry** — Host RPM packages with `repomd.xml` metadata for `dnf`/`yum`
- **Chef Registry** — Host Chef cookbooks with Chef Supermarket-compatible API
- **Generic Packages** — Upload and download arbitrary binary artifacts via REST API

### Static Sites
- **Pages** — Serve static websites directly from a repository branch (like GitHub Pages) at `/pages/{owner}/{repo}/`

### RSS/Atom Feeds
- **Repository Feeds** — Atom feeds for commits, releases, and tags per repository (`/api/feeds/{repo}/commits.atom`, `/api/feeds/{repo}/releases.atom`, `/api/feeds/{repo}/tags.atom`)
- **User Activity Feed** — Per-user activity feed (`/api/feeds/users/{username}/activity.atom`)
- **Global Activity Feed** — Site-wide activity feed (`/api/feeds/global/activity.atom`)

### Notifications
- **In-App Notifications** — Mentions, comments, and repository activity
- **Push Notifications** — Ntfy and Gotify integration for real-time mobile/desktop alerts with per-user opt-in

### Authentication
- **OAuth2 / SSO** — Sign in with GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, or Twitter/X. Admins configure Client ID and Secret per provider in the Admin dashboard — only providers with credentials filled in are shown to users
- **OAuth2 Provider** — Act as an identity provider so other apps can use "Sign in with MyPersonalGit". Implements Authorization Code flow with PKCE, token refresh, userinfo endpoint, and OpenID Connect discovery (`.well-known/openid-configuration`)
- **LDAP / Active Directory** — Authenticate users against an LDAP directory or Active Directory domain. Users are auto-provisioned on first login with synced attributes (email, display name). Supports group-based admin promotion, SSL/TLS, and StartTLS
- **SSPI / Windows Integrated Auth** — Transparent Single Sign-On for Windows domain users via Negotiate/NTLM. Users on a domain are authenticated automatically without entering credentials. Enable in Admin > Settings (Windows only)
- **Two-Factor Authentication** — TOTP-based 2FA with authenticator app support and recovery codes
- **WebAuthn / Passkeys** — FIDO2 hardware security key and passkey support as a second factor. Register YubiKeys, platform authenticators (Face ID, Windows Hello, Touch ID), and other FIDO2 devices. Sign count verification for cloned key detection
- **Linked Accounts** — Users can link multiple OAuth providers to their account from Settings

### Administration
- **Admin Dashboard** — System settings (including database provider, TLS/HTTPS, SSH server, LDAP/AD, footer pages), user management, audit logs, and statistics organized into separate cards per section
- **Built-in TLS/HTTPS** — Enable HTTPS directly from admin settings with three certificate options: self-signed (auto-generated, 2-year validity), PFX/PKCS#12 file, or PEM cert+key pair (e.g., Let's Encrypt). Configurable internal/external ports for Docker port mapping, optional HTTP-to-HTTPS redirect
- **Customizable Footer Pages** — Terms of Service, Privacy Policy, Documentation, and Contact pages with Markdown content editable from Admin > Settings
- **User Profiles** — Contribution heatmap, activity feed, and stats per user
- **Gravatar Avatars** — User avatars throughout the UI use Gravatar identicons based on username, with automatic fallback
- **Personal Access Tokens** — Token-based API authentication with configurable scopes and optional route-level restrictions (glob patterns like `/api/packages/**` to limit token access to specific API paths)
- **Deploy Keys** — Repository-scoped SSH keys for CI/CD and automation, with read-only or read-write access per key
- **Backup & Restore** — Export and import server data
- **Security Scanning** — Real dependency vulnerability scanning powered by the [OSV.dev](https://osv.dev/) database. Automatically extracts dependencies from `.csproj` (NuGet), `package.json` (npm), `requirements.txt` (PyPI), `Cargo.toml` (Rust), `Gemfile` (Ruby), `composer.json` (PHP), `go.mod` (Go), `pom.xml` (Maven/Java), and `pubspec.yaml` (Dart/Flutter), then checks each against known CVEs. Reports severity, fixed versions, and advisory links. Plus manual security advisories with draft/publish/close workflow
- **Secret Scanning** — Automatically scans every push for leaked credentials (AWS keys, GitHub/GitLab tokens, Slack tokens, private keys, API keys, JWTs, connection strings, and more). 20 built-in patterns with full regex support. Full repository scan on demand. Alerts with resolve/false-positive workflow. Custom patterns configurable via API
- **Dependabot-Style Auto-Update PRs** — Automatically check for outdated dependencies and create pull requests to update them. Supports NuGet, npm, and PyPI ecosystems. Configurable schedule (daily/weekly/monthly) and open PR limit per repository
- **Repository Insights (Traffic)** — Track clone/fetch counts, page views, unique visitors, top referrers, and popular content paths. Traffic charts in the Insights tab with 14-day summaries. Daily aggregation with 90-day retention. IP addresses are hashed for privacy
- **Dark Mode** — Full dark/light mode support with a toggle in the header
- **Multi-Language / i18n** — Full localization across all 30+ pages including the Web IDE with 1,088 resource keys. Ships with 11 languages: English, Spanish, French, German, Japanese, Korean, Chinese (Simplified), Portuguese, Russian, Italian, and Turkish. Language picker in the header and IDE top bar. Add more by creating `SharedResource.{locale}.resx` files
- **Swagger / OpenAPI** — Interactive API documentation at `/swagger` with all REST endpoints discoverable and testable
- **Open Graph Meta Tags** — Repository, issue, and PR pages include og:title and og:description for rich link previews in Slack, Discord, and social media
- **Emoji Shortcodes** — GitHub-style emoji shortcodes (`:white_check_mark:`, `:rocket:`, etc.) rendered as actual emoji throughout all Markdown views
- **Mermaid Diagrams** — Mermaid diagram rendering in Markdown files (flowcharts, sequence diagrams, Gantt charts, etc.)
- **Math Rendering** — LaTeX/KaTeX math expressions in Markdown (`$inline$` and `$$display$$` syntax)
- **CSV/TSV Viewer** — CSV and TSV files render as formatted, sortable tables instead of raw text
- **Keyboard Shortcuts** — Press `?` for a shortcuts help modal. `/` focuses search, `g i` goes to Issues, `g p` to Pull Requests, `g h` to Home, `g n` to Notifications
- **Health Check Endpoint** — `/health` returns JSON with database connectivity status for Docker/Kubernetes monitoring
- **Sitemap.xml** — Dynamic XML sitemap at `/sitemap.xml` listing all public repositories for search engine indexing
- **Line Linking** — Click line numbers in file viewer to generate shareable `#L42` URLs with line highlighting on load
- **File Download** — Download individual files from the file viewer with proper Content-Disposition headers
- **Rate Limit Headers** — API responses include `X-RateLimit-Limit` and `X-RateLimit-Reset` headers so clients can track their remaining quota
- **Notification Auto-Refresh** — The notification bell badge auto-updates every 60 seconds without requiring a page refresh
- **Jupyter Notebook Rendering** — `.ipynb` files render as formatted notebooks with code cells, Markdown, outputs, and inline images
- **Repository Transfer** — Transfer repository ownership to another user or organization from repository Settings
- **Default Branch Configuration** — Change the default branch per repository from the Settings tab
- **Rename Repository** — Rename a repository from Settings with automatic update of all references (issues, PRs, stars, webhooks, secrets, etc.)
- **User-Level Secrets** — Encrypted secrets shared across all repositories owned by a user, managed from Settings > Secrets
- **Organization-Level Secrets** — Encrypted secrets shared across all repositories in an organization, managed from the org's Secrets tab
- **Repository Pinning** — Pin up to 6 favorite repositories to your user profile page for quick access
- **Git Hooks Management** — Web UI to view, edit, and manage server-side Git hooks (pre-receive, update, post-receive, post-update, pre-push) per repository
- **Protected File Patterns** — Branch protection rule with glob patterns to require review approval for changes to specific files (e.g., `*.lock`, `migrations/**`, `.github/workflows/*`)
- **External Issue Tracker** — Configure repositories to link to an external issue tracker (Jira, Linear, etc.) with custom URL patterns instead of built-in issues
- **Federation (NodeInfo/WebFinger)** — NodeInfo 2.0 discovery at `/.well-known/nodeinfo`, WebFinger at `/.well-known/webfinger`, and host-meta for cross-instance discoverability
- **Distributed CI Runners** — External runners can register via API, poll for queued jobs, and report results. Enables distributed CI execution across multiple machines alongside the built-in Docker runner

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | ASP.NET Core 10.0 |
| Frontend | Blazor Server (interactive server-side rendering) |
| Database | SQLite (default) or PostgreSQL via Entity Framework Core 10 |
| Git Engine | LibGit2Sharp |
| Auth | BCrypt password hashing, session-based auth, PAT tokens, OAuth2 (8 providers + provider mode), TOTP 2FA, WebAuthn/Passkeys, LDAP/AD, SSPI |
| SSH Server | Built-in SSH2 protocol implementation (ECDH, AES-CTR, HMAC-SHA2) |
| Markdown | Markdig |
| CI/CD | Docker.DotNet, YamlDotNet |
| Monitoring | Prometheus metrics |

## Quick Start

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) (recommended)
- Or [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) + Git for local development

### Docker (Recommended)

Pull from Docker Hub and run:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v mypersonalgit-repos:/repos \
  -v mypersonalgit-data:/data \
  -e Git__Users__admin=admin \
  fennch/mypersonalgit:latest
```

> Port 2222 is optional — only needed if you enable the built-in SSH server in Admin > Settings.

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
| `Database__Provider` | Database engine: `sqlite` or `postgresql` | `sqlite` |
| `ConnectionStrings__Default` | Database connection string | `Data Source=/data/mypersonalgit.db` |
| `Git__ProjectRoot` | Directory where Git repos are stored | `/repos` |
| `Git__RequireAuth` | Require auth for Git HTTP operations | `true` |
| `Git__Users__<username>` | Set password for Git HTTP Basic Auth user | — |
| `RESET_ADMIN_PASSWORD` | Emergency admin password reset on startup | — |
| `Secrets__EncryptionKey` | Custom encryption key for repository secrets | Derived from DB connection string |
| `Ssh__DataDir` | Directory for SSH data (host keys, authorized_keys) | `~/.mypersonalgit/ssh` |
| `Ssh__AuthorizedKeysPath` | Path to generated authorized_keys file | `<DataDir>/authorized_keys` |

> **Note:** The built-in SSH server port and LDAP settings are configured through the Admin dashboard (Admin > Settings), not environment variables. This lets you change them without redeploying.

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

**PyPI (Python packages):**
```bash
# Install a package
pip install mypackage --index-url http://localhost:8080/api/packages/pypi/simple/

# Upload with twine
pip install twine
cat > ~/.pypirc << 'EOF'
[distutils]
index-servers = mygit

[mygit]
repository = http://localhost:8080/api/packages/pypi/upload/
username = youruser
password = yourPAT
EOF
twine upload --repository mygit dist/*
```

**Maven (Java/JVM packages):**
```xml
<!-- In your pom.xml, add the repository -->
<distributionManagement>
  <repository>
    <id>mygit</id>
    <url>http://localhost:8080/api/packages/maven</url>
  </repository>
</distributionManagement>
```
```xml
<!-- In settings.xml, add credentials -->
<server>
  <id>mygit</id>
  <username>youruser</username>
  <password>yourPAT</password>
</server>
```
```bash
mvn deploy
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

### 10. SSH Key Authentication

Use SSH keys for passwordless Git operations. There are two options:

#### Option A: Built-in SSH Server (Recommended)

No external SSH daemon required — MyPersonalGit runs its own SSH server:

1. Go to **Admin > Settings** and enable **Built-in SSH Server**
2. Set the SSH port (default: 2222) — use 22 if you're not running system SSH
3. Save settings and restart the server (port changes require restart)
4. Go to **Settings > SSH Keys** and add your public key (`~/.ssh/id_ed25519.pub`, `~/.ssh/id_rsa.pub`, or `~/.ssh/id_ecdsa.pub`)
5. Clone via SSH:
   ```bash
   git clone ssh://youruser@yourserver:2222/MyRepo.git
   ```

The built-in SSH server supports ECDH-SHA2-NISTP256 key exchange, AES-128/256-CTR encryption, HMAC-SHA2-256, and public key authentication with Ed25519, RSA, and ECDSA keys.

#### Option B: System OpenSSH

If you prefer to use your system's SSH daemon:

1. Go to **Settings > SSH Keys** and add your public key
2. MyPersonalGit automatically maintains an `authorized_keys` file from all registered SSH keys
3. Configure your server's OpenSSH to use the generated authorized_keys file:
   ```
   # In /etc/ssh/sshd_config
   AuthorizedKeysFile /path/to/.mypersonalgit/ssh/authorized_keys
   ```
4. Clone via SSH:
   ```bash
   git clone ssh://git@yourserver:22/repos/MyRepo.git
   ```

The SSH auth service also exposes an API at `/api/ssh/authorized-keys` for use with OpenSSH's `AuthorizedKeysCommand` directive.

### 11. LDAP / Active Directory Authentication

Authenticate users against your organization's LDAP directory or Active Directory domain:

1. Go to **Admin > Settings** and scroll to **LDAP / Active Directory Authentication**
2. Enable LDAP and fill in your server details:
   - **Server**: Your LDAP server hostname (e.g., `dc01.corp.local`)
   - **Port**: 389 for LDAP, 636 for LDAPS
   - **SSL/TLS**: Enable for LDAPS, or use StartTLS for upgrading a plain connection
3. Configure a service account for searching users:
   - **Bind DN**: `CN=svc-git,OU=Service Accounts,DC=corp,DC=local`
   - **Bind Password**: The service account password
4. Set the search parameters:
   - **Search Base DN**: `OU=Users,DC=corp,DC=local`
   - **User Filter**: `(sAMAccountName={0})` for AD, `(uid={0})` for OpenLDAP
5. Map LDAP attributes to user fields:
   - **Username**: `sAMAccountName` (AD) or `uid` (OpenLDAP)
   - **Email**: `mail`
   - **Display Name**: `displayName`
6. Optionally set an **Admin Group DN** — members of this group are automatically promoted to admin
7. Click **Test LDAP Connection** to verify the settings
8. Save settings

Users can now sign in with their domain credentials on the login page. On first login, a local account is auto-created with attributes synced from the directory. LDAP authentication is also used for Git HTTP operations (clone/push).

### 12. Repository Secrets

Add encrypted secrets to repositories for use in CI/CD workflows:

1. Go to your repository's **Settings** tab
2. Scroll to the **Secrets** card and click **Add secret**
3. Enter a name (e.g., `DEPLOY_TOKEN`) and value — the value is encrypted with AES-256
4. Secrets are automatically injected as environment variables into every workflow run

Reference secrets in your workflow:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Deploy
        run: curl -H "Authorization: Bearer $DEPLOY_TOKEN" https://api.example.com/deploy
```

### 13. OAuth / SSO Login

Sign in with external identity providers:

1. Go to **Admin > OAuth / SSO** and configure the providers you want to enable
2. Enter the **Client ID** and **Client Secret** from the provider's developer console
3. Check **Enable** — only providers with both credentials filled in will appear on the login page
4. The callback URL for each provider is shown in the admin panel (e.g., `https://yourserver/oauth/callback/github`)

Supported providers: GitHub, Google, Microsoft, GitLab, Bitbucket, Facebook, Discord, Twitter/X.

Users can link multiple providers to their account in **Settings > Linked Accounts**.

### 14. Import Repository

Import repositories from external sources with full history:

1. Click **Import** on the home page
2. Select a source type (Git URL, GitHub, GitLab, or Bitbucket)
3. Enter the repository URL and optionally an auth token for private repos
4. For GitHub/GitLab/Bitbucket imports, optionally import issues and pull requests
5. Track import progress in real-time on the Import page

### 15. Forking & Upstream Sync

Fork a repository and keep it in sync:

1. Click the **Fork** button on any repository page
2. A fork is created under your username with a link back to the original
3. Click **Sync fork** next to the "forked from" badge to pull latest changes from upstream

### 16. CI/CD Auto-Release

MyPersonalGit includes a built-in CI/CD pipeline that auto-tags, releases, and pushes Docker images on every push to main. Workflows auto-trigger on push — no external CI service needed.

**How it works:**
1. Push to `main` auto-triggers `.github/workflows/release.yml`
2. Bumps the patch version (`v1.15.1` -> `v1.15.2`), creates a git tag
3. Logs into Docker Hub, builds the image, and pushes it as both `:latest` and `:vX.Y.Z`

**Setup:**
1. Go to your repo's **Settings > Secrets** in MyPersonalGit
2. Add a secret named `DOCKERHUB_TOKEN` with your Docker Hub access token
3. Make sure the MyPersonalGit container has the Docker socket mounted (`-v /var/run/docker.sock:/var/run/docker.sock`)
4. Push to main — the workflow triggers automatically

**GitHub Actions compatibility:**
The same workflow YAML also works on GitHub Actions — no changes needed. MyPersonalGit translates `uses:` actions into equivalent shell commands at runtime:

| GitHub Action | MyPersonalGit Translation |
|---|---|
| `actions/checkout@v4` | Repo already cloned to `/workspace` |
| `actions/setup-dotnet@v4` | Installs .NET SDK via official install script |
| `actions/setup-node@v4` | Installs Node.js via NodeSource |
| `actions/setup-python@v5` | Installs Python via apt/apk |
| `actions/setup-java@v4` | Installs OpenJDK via apt/apk |
| `docker/login-action@v3` | `docker login` with stdin password |
| `docker/build-push-action@v6` | `docker build && docker push` |
| `docker/setup-buildx-action@v3` | No-op (uses default builder) |
| `softprops/action-gh-release@v2` | Creates a real Release entity in the database |
| `${{ secrets.X }}` | `$X` environment variable |
| `${{ steps.X.outputs.Y }}` | `$Y` environment variable |
| `${{ github.sha }}` | `$GITHUB_SHA` environment variable |

**Parallel jobs:**
Jobs run in parallel by default. Use `needs:` to declare dependencies:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: dotnet build

  test:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - run: dotnet test

  deploy:
    needs: [build, test]
    runs-on: ubuntu-latest
    steps:
      - run: echo "deploying..."
```
Jobs with no `needs:` start immediately. A job is cancelled if any of its dependencies fail.

**Conditional steps:**
Use `if:` to control when steps run:
```yaml
steps:
  - name: Build
    run: dotnet build

  - name: Notify on failure
    if: failure()
    run: curl -X POST https://hooks.example.com/alert

  - name: Cleanup
    if: always()
    run: rm -rf ./tmp
```
Supported expressions: `always()`, `success()` (default), `failure()`, `cancelled()`, `true`, `false`.

**Step outputs:**
Steps can pass values to subsequent steps via `$GITHUB_OUTPUT`:
```yaml
steps:
  - name: Determine version
    run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  - name: Use version
    run: echo "Building version $version"
```

**Matrix builds:**
Fan out jobs across multiple combinations using `strategy.matrix`:
```yaml
jobs:
  test:
    strategy:
      fail-fast: true
      matrix:
        os: [ubuntu-latest, node-20]
        version: ["1.0", "2.0"]
    runs-on: ${{ matrix.os }}
    steps:
      - run: echo "Testing on ${{ matrix.os }} with version ${{ matrix.version }}"
```
This creates 4 jobs: `test (ubuntu-latest, 1.0)`, `test (ubuntu-latest, 2.0)`, etc. All run in parallel.

**Manual triggers with inputs (`workflow_dispatch`):**
Define typed inputs that show as a form in the UI when triggering manually:
```yaml
on:
  workflow_dispatch:
    inputs:
      environment:
        description: "Target environment"
        required: true
        type: choice
        options:
          - staging
          - production
      debug:
        description: "Enable debug mode"
        type: boolean
        default: "false"

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - run: echo "Deploying to $INPUT_ENVIRONMENT (debug=$INPUT_DEBUG)"
```
Input values are injected as `INPUT_<NAME>` environment variables (uppercased).

**Job timeouts:**
Set `timeout-minutes` on jobs to automatically fail them if they run too long:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - run: make build
```
Default timeout is 360 minutes (6 hours), matching GitHub Actions.

**Job-level conditionals:**
Use `if:` on jobs to skip them based on conditions:
```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - run: dotnet test

  deploy:
    needs: test
    if: success()
    runs-on: ubuntu-latest
    steps:
      - run: echo "deploying..."

  notify:
    needs: test
    if: failure()
    runs-on: ubuntu-latest
    steps:
      - run: curl -X POST https://hooks.example.com/alert
```

**Job outputs:**
Jobs can pass values to downstream jobs via `outputs:`:
```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.ver.outputs.version }}
    steps:
      - id: ver
        run: echo "version=1.2.3" >> $GITHUB_OUTPUT

  deploy:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - run: echo "Deploying version $version"
```

**Continue on error:**
Let a step fail without failing the job:
```yaml
steps:
  - name: Optional lint
    continue-on-error: true
    run: npm run lint

  - name: Build (always runs)
    run: npm run build
```

**Path filtering:**
Only trigger workflows when specific files change:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'src/**'
      - '*.csproj'
    # or use paths-ignore:
    # paths-ignore:
    #   - 'docs/**'
    #   - '*.md'
```

**Working directory:**
Set where commands execute:
```yaml
defaults:
  run:
    working-directory: src/app

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: npm install          # runs in src/app
      - run: npm test
        working-directory: tests  # overrides default
```

**Re-run workflows:**
Click the **Re-run** button on any completed, failed, or cancelled workflow run to create a fresh run with the same jobs, steps, and configuration.

**Pull request workflows:**
Workflows with `on: pull_request` auto-trigger when a non-draft PR is created, running checks against the source branch.

**Commit status checks:**
Workflows automatically set commit statuses (pending/success/failure) so you can see build results on PRs and enforce required checks via branch protection.

**Workflow cancellation:**
Click the **Cancel** button on any running or queued workflow in the Actions UI to stop it immediately.

**Status badges:**
Embed build status badges in your README or anywhere:
```markdown
![Build](http://your-server/api/badge/YourRepo/workflow)
![Status](http://your-server/api/badge/YourRepo/status)
```
Filter by workflow name: `/api/badge/YourRepo/workflow?workflow=Release%20%26%20Docker%20Push`

### 17. RSS/Atom Feeds

Subscribe to repository activity using standard Atom feeds in any RSS reader:

```
# Repository commits
http://localhost:8080/api/feeds/MyRepo/commits.atom

# Repository releases
http://localhost:8080/api/feeds/MyRepo/releases.atom

# Repository tags
http://localhost:8080/api/feeds/MyRepo/tags.atom

# User activity
http://localhost:8080/api/feeds/users/admin/activity.atom

# Global activity (all repositories)
http://localhost:8080/api/feeds/global/activity.atom
```

No authentication required for public repositories. Add these URLs to any feed reader (Feedly, Miniflux, FreshRSS, etc.) to stay notified of changes.

## Database Configuration

MyPersonalGit uses **SQLite** by default — zero configuration, single-file database, perfect for personal use and small teams.

For larger deployments (many concurrent users, high availability, or if you already run PostgreSQL), you can switch to **PostgreSQL**:

### Using PostgreSQL

**Docker Compose** (recommended for PostgreSQL):
```yaml
services:
  mypersonalgit:
    image: fennch/mypersonalgit:latest
    ports:
      - "8080:8080"
      - "2222:2222"
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

**Environment variables only** (if you already have a PostgreSQL server):
```bash
docker run -d --name mypersonalgit -p 8080:8080 \
  -v mypersonalgit-repos:/repos \
  -e Database__Provider=postgresql \
  -e ConnectionStrings__Default="Host=your-pg-server;Database=mypersonalgit;Username=mypg;Password=secret" \
  fennch/mypersonalgit:latest
```

EF Core migrations run automatically on startup for both providers. No manual schema setup required.

### Switching from the Admin Dashboard

You can also switch database providers directly from the web UI:

1. Go to **Admin > Settings** — the **Database** card is at the top
2. Select **PostgreSQL** from the provider dropdown
3. Enter your PostgreSQL connection string (e.g., `Host=localhost;Database=mypersonalgit;Username=mypg;Password=secret`)
4. Click **Save Database Settings**
5. Restart the application for the change to take effect

The config is saved to `~/.mypersonalgit/database.json` (outside the database itself, so it can be read before connecting).

### Choosing a Database

| | SQLite | PostgreSQL |
|---|---|---|
| **Setup** | Zero config (default) | Requires a PostgreSQL server |
| **Best for** | Personal use, small teams, NAS | Teams of 50+, high concurrency |
| **Backup** | Copy the `.db` file | Standard `pg_dump` |
| **Concurrency** | Single-writer (fine for most use) | Full multi-writer |
| **Migration** | N/A | Switch provider + run the app (auto-migrates) |

## Deploy to a NAS

MyPersonalGit works great on a NAS (QNAP, Synology, etc.) via Docker:

```bash
docker run -d --name mypersonalgit -p 8080:8080 -p 2222:2222 \
  -v /share/Container/mypersonalgit/repos:/repos \
  -v /share/Container/mypersonalgit/data:/data \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ConnectionStrings__Default="Data Source=/data/mypersonalgit.db" \
  -e Git__Users__admin=yourpassword \
  fennch/mypersonalgit:latest
```

The Docker socket mount is optional — only needed if you want CI/CD workflow execution. Port 2222 is only needed if you enable the built-in SSH server.

## Configuration

All settings can be configured in `appsettings.json`, via environment variables, or through the Admin dashboard at `/admin`:

- Database provider (SQLite or PostgreSQL)
- Project root directory
- Authentication requirements
- User registration settings
- Feature toggles (Issues, Wiki, Projects, Actions)
- Max repository size and count per user
- SMTP settings for email notifications
- Push notification settings (Ntfy/Gotify)
- Built-in SSH server (enable/disable, port)
- LDAP/Active Directory authentication (server, bind DN, search base, user filter, attribute mapping, admin group)
- OAuth/SSO provider configuration (Client ID/Secret per provider)

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
    SshServer/       # Built-in SSH server (SSH2 protocol, ECDH, AES-CTR)
  Program.cs         # App startup, DI, middleware pipeline
MyPersonalGit.Tests/
  UnitTest1.cs       # xUnit tests with InMemory database
```

## Running Tests

```bash
dotnet test
```

## Contributing

Contributions are welcome! Please read the [Contributing Guide](CONTRIBUTING.md) for details on setting up the development environment, code style, and how to submit pull requests.

## License

MIT