# Contributing to MyPersonalGit

Thanks for your interest in contributing! This guide will help you get started.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git](https://git-scm.com/)
- [Docker](https://docs.docker.com/get-docker/) (optional, for CI/CD runner testing)

### Setting Up the Development Environment

```bash
git clone https://github.com/ChrisDFennell/MyPersonalGit.git
cd MyPersonalGit/MyPersonalGit
dotnet run
```

The app starts at `http://localhost:5146`. Default credentials: `admin` / `admin`.

### Running Tests

```bash
dotnet test
```

## How to Contribute

### Reporting Bugs

- Check [existing issues](https://github.com/ChrisDFennell/MyPersonalGit/issues) first
- Include steps to reproduce, expected behavior, and actual behavior
- Include your .NET version, OS, and browser

### Suggesting Features

- Open an issue with the `enhancement` label
- Describe the use case and why it would be useful
- If possible, reference how other platforms (GitHub, Gitea, GitLab) handle it

### Submitting Pull Requests

1. Fork the repository
2. Create a feature branch from `main`: `git checkout -b feature/my-feature`
3. Make your changes
4. Run the tests: `dotnet test`
5. Commit with a clear message describing what and why
6. Push and open a PR against `main`

### Code Style

- Follow existing patterns in the codebase
- Use the same naming conventions (PascalCase for public, camelCase for private)
- Add i18n resource keys for any user-visible strings (see `Resources/SharedResource.resx`)
- Add translations to all 10 locale files when adding new strings
- Keep Blazor pages self-contained where possible
- Services should use `IDbContextFactory<AppDbContext>` (not scoped injection)

### Project Structure

```
MyPersonalGit/
  Components/Pages/     Blazor pages (one per feature area)
  Controllers/          REST API endpoints
  Data/                 Services + EF Core DbContext
  Models/               Domain models
  Resources/            i18n .resx files (11 languages)
  Services/             Middleware (auth, Git HTTP, SSH)
MyPersonalGit.Tests/    xUnit tests
```

### Adding a New Feature

1. **Model** — Add to `Models/` if new entities are needed
2. **DbContext** — Add `DbSet<>` to `AppDbContext.cs` + `OnModelCreating` config
3. **Migration** — Add `CREATE TABLE IF NOT EXISTS` SQL to `Program.cs` (we don't use EF migration files)
4. **Service** — Add interface + implementation to `Data/`, register in `Program.cs`
5. **Controller** — Add to `Controllers/` if API endpoints are needed
6. **UI** — Add/modify Blazor pages in `Components/Pages/`
7. **i18n** — Add resource keys to all 11 `.resx` files
8. **Tests** — Add tests to `MyPersonalGit.Tests/`

## Code of Conduct

Be respectful, constructive, and inclusive. We're all here to build something useful.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
