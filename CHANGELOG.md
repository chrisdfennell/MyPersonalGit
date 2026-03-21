# Changelog

All notable changes to MyPersonalGit are documented here.

## [Unreleased]

### Added
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
