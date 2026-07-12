using Microsoft.EntityFrameworkCore;

namespace MyPersonalGit.Data;

/// <summary>
/// FTS5 shadow table over CodeSearchIndices (SQLite only). The trigram tokenizer
/// gives case-insensitive substring matching — same semantics as the LIKE fallback
/// but index-backed. Triggers keep it in sync so the indexer service needs no changes.
/// </summary>
public static class CodeSearchFts
{
    /// <summary>Create the FTS table + sync triggers and backfill if out of sync. Idempotent.</summary>
    public static void EnsureCreated(AppDbContext db, Action<string> log)
    {
        if (!db.Database.IsSqlite()) return;

        db.Database.ExecuteSqlRaw(@"
            CREATE VIRTUAL TABLE IF NOT EXISTS ""CodeSearchFts"" USING fts5(
                ""Content"",
                content='CodeSearchIndices',
                content_rowid='Id',
                tokenize='trigram'
            );");
        db.Database.ExecuteSqlRaw(@"
            CREATE TRIGGER IF NOT EXISTS ""CodeSearchIndices_fts_ai"" AFTER INSERT ON ""CodeSearchIndices"" BEGIN
                INSERT INTO ""CodeSearchFts""(rowid, ""Content"") VALUES (new.""Id"", new.""Content"");
            END;");
        db.Database.ExecuteSqlRaw(@"
            CREATE TRIGGER IF NOT EXISTS ""CodeSearchIndices_fts_ad"" AFTER DELETE ON ""CodeSearchIndices"" BEGIN
                INSERT INTO ""CodeSearchFts""(""CodeSearchFts"", rowid, ""Content"") VALUES ('delete', old.""Id"", old.""Content"");
            END;");
        db.Database.ExecuteSqlRaw(@"
            CREATE TRIGGER IF NOT EXISTS ""CodeSearchIndices_fts_au"" AFTER UPDATE ON ""CodeSearchIndices"" BEGIN
                INSERT INTO ""CodeSearchFts""(""CodeSearchFts"", rowid, ""Content"") VALUES ('delete', old.""Id"", old.""Content"");
                INSERT INTO ""CodeSearchFts""(rowid, ""Content"") VALUES (new.""Id"", new.""Content"");
            END;");

        // Backfill rows indexed before the FTS table existed.
        var conn = db.Database.GetDbConnection(); // owned by the context — do not dispose
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
        long missing;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"SELECT (SELECT COUNT(*) FROM ""CodeSearchIndices"") - (SELECT COUNT(*) FROM ""CodeSearchFts"")";
            missing = Convert.ToInt64(cmd.ExecuteScalar());
        }
        if (missing != 0)
        {
            db.Database.ExecuteSqlRaw(@"INSERT INTO ""CodeSearchFts""(""CodeSearchFts"") VALUES ('rebuild');");
            log($"==> Rebuilt full-text code search index ({missing} row(s) were out of sync)");
        }
    }
}
