# Changelog

All notable changes to MyPersonalGit are documented here.

## [Unreleased]

### Added
- Web IDE: Drag-to-resize panels — sidebar and bottom panel are draggable with visual resize handles
- Web IDE: File nesting — related files group together (e.g., WebIde.razor + .css + .cs) with expandable chevrons and count badges
- Web IDE: CSS color previews — inline color swatches next to hex/rgb/hsl values in CSS/SCSS/Less files
- Web IDE: Git commit graph — visual branch/merge timeline with colored lane lines, branch labels, and commit details
- Web IDE: Minimap highlights — modified lines (blue), added lines (green), and conflict markers (red) shown in minimap gutter
- Built-in TLS/HTTPS — enable HTTPS directly from admin settings with three cert options: self-signed (auto-generated), PFX file, or PEM cert+key (Let's Encrypt). Includes HTTP-to-HTTPS redirect toggle. Port 8443 exposed by default.
- Web IDE: Git Branch Creation — create new branches from the IDE branch dropdown with source branch selection
- Web IDE: Merge Conflict Resolution — inline Accept Current / Accept Incoming / Accept Both buttons with color-coded conflict regions
- Web IDE: Tab Context Menu — Close Tabs to the Right and Close Saved Tabs actions
- Web IDE: Font Family Picker — choose between Cascadia Code, Fira Code, JetBrains Mono, Consolas, and more
- Web IDE: Auto-Save — optional auto-commit with configurable delay (500ms–5s), toggle in settings or command palette
- Web IDE: Persistent Workspace — remembers open tabs, pinned tabs, active file, sidebar & panel state across sessions
- Web IDE: Pinned Tabs — right-click to pin/unpin tabs; pinned tabs sort to the left and can't be accidentally closed
- Web IDE: Tab Overflow Handling — scroll chevrons appear when tabs overflow the tab bar
- Web IDE: Tab Context Menu — right-click tabs for Pin, Close, and Close Other Tabs actions
- Web IDE: Format on Paste — auto-reindent pasted code to match surrounding context (toggleable in settings)
- Web IDE: Emmet Support — HTML/CSS abbreviation expansion in HTML, Razor, CSS, SCSS, and Less files
- Web IDE: Code Folding — collapse/expand code blocks with always-visible fold controls
- Web IDE: Bracket Matching & Indent Guides — enhanced bracket pair colorization, active bracket highlighting, and indentation guides
- Web IDE: Word Wrap Toggle — status bar button to toggle word wrap on/off
- Web IDE: Outline/Symbol Panel — sidebar panel showing classes, methods, and properties with click-to-navigate
- Web IDE: Toast Notifications — non-intrusive feedback for commits, file operations, and errors
- Web IDE: Zen Mode — distraction-free editing that hides sidebar, tabs, and panels (Escape to exit)
- Web IDE: Enhanced Settings — code folding, sticky scroll, bracket guides, and font ligatures toggles
- Web IDE: Command Palette additions — zen mode, word wrap toggle, fold/unfold all, outline panel
- TODO Scanner — scan code for TODO/FIXME/HACK/XXX comments with filterable dashboard
- Repository Health Score — A-F grade with checklist (README, LICENSE, CI, etc.)
- Repo Size Analytics — directory breakdown, largest files, per-extension sizes
- Saved Replies — canned responses for issue/PR comments
- Batch Issue Operations — close/reopen multiple issues at once
- Squash Commit Message — customize message when squash-merging PRs
- Branch Delete After Merge — auto-delete source branch option
- CI Status on PR List — green/red/yellow icons per PR
- Star from Explore — star repos without opening them
- @Mention Notifications — notify users when mentioned in comments
- Gravatar Avatars — identicon avatars throughout the UI
- Keyboard Shortcuts — ? for help, / for search, g+key navigation
- Activity Pulse — weekly summary page per repo
- Notification Auto-Refresh — bell badge polls every 60s
- Health Check Endpoint — /health with DB connectivity status
- Sitemap.xml — dynamic sitemap for SEO
- robots.txt — sensible defaults for search engines
- Open Graph Meta Tags — rich link previews for repos, issues, PRs
- Line Linking — click line numbers for #L42 shareable URLs
- File Download — individual file download with Content-Disposition
- Rate Limit Headers — X-RateLimit-* on API responses
- License Detection — auto-detect MIT/Apache/GPL/BSD in sidebar
- Release Editing — edit releases after creation
- CONTRIBUTING.md Awareness — alert in new issue modal
- Secret Scanning — 20 patterns, push scanning, full scan, resolve workflow
- Dependabot Auto-Update PRs — NuGet/npm/PyPI with configurable schedule
- Repository Traffic Insights — clone/view counts, referrers, daily aggregation
- Auto-Merge — merge PRs when checks pass and reviews approve
- Reusable Workflows — workflow_call with inputs/outputs/secrets
- Composite Actions — multi-step actions expanded inline
- Environment Deployments — required reviewers, wait timers, branch restrictions
- Cherry-Pick / Revert via UI — direct or as PR
- Transfer Issues — move between repos with comments and labels
- Platform Stats page — instance-wide statistics

### Fixed
- Workflow trigger .git suffix mismatch
- Auto-merge button visibility on all open PRs
- Workflow queries match both RepoName and RepoName.git variants

## [1.15.71] - 2026-03-20

- Initial tagged release with 20 package registries, CI/CD, OAuth, LDAP, WebAuthn, and more
- See [README.md](README.md) for full feature list
