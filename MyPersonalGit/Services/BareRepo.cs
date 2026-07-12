namespace MyPersonalGit.Services;

/// <summary>Creates bare repositories with HEAD pointing at main.</summary>
public static class BareRepo
{
    /// <summary>
    /// Bare init points HEAD at master, but pushes and UI-generated initial commits
    /// use main — leaving HEAD on a branch that never comes to exist, so fresh
    /// clones check out nothing. Re-target HEAD at creation time.
    /// </summary>
    public static void Create(string path)
    {
        LibGit2Sharp.Repository.Init(path, isBare: true);
        using var repo = new LibGit2Sharp.Repository(path);
        repo.Refs.UpdateTarget("HEAD", "refs/heads/main");
    }
}
