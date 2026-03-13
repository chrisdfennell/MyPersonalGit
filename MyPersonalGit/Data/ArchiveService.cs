using System.Formats.Tar;
using System.IO.Compression;
using LibGit2Sharp;

namespace MyPersonalGit.Data;

public enum ArchiveFormat
{
    Zip,
    TarGz
}

public interface IArchiveService
{
    /// <summary>
    /// Creates an archive (ZIP or TAR.GZ) of the repository content at the given ref.
    /// Returns a MemoryStream positioned at 0, or null if the ref/repo is invalid.
    /// </summary>
    MemoryStream? CreateArchive(string repoPath, string? gitRef, ArchiveFormat format, out string resolvedRef);
}

public class ArchiveService : IArchiveService
{
    private readonly ILogger<ArchiveService> _logger;

    public ArchiveService(ILogger<ArchiveService> logger)
    {
        _logger = logger;
    }

    public MemoryStream? CreateArchive(string repoPath, string? gitRef, ArchiveFormat format, out string resolvedRef)
    {
        resolvedRef = "";

        if (!Repository.IsValid(repoPath))
            return null;

        using var repo = new Repository(repoPath);

        // Resolve the ref to a commit
        var commit = ResolveCommit(repo, gitRef);
        if (commit == null)
            return null;

        // Determine a friendly name for the resolved ref
        resolvedRef = gitRef ?? repo.Head?.FriendlyName ?? commit.Id.ToString(7);

        var ms = new MemoryStream();

        switch (format)
        {
            case ArchiveFormat.Zip:
                WriteZip(ms, commit.Tree, resolvedRef);
                break;
            case ArchiveFormat.TarGz:
                WriteTarGz(ms, commit.Tree, resolvedRef);
                break;
        }

        ms.Position = 0;
        return ms;
    }

    private static Commit? ResolveCommit(Repository repo, string? gitRef)
    {
        if (string.IsNullOrEmpty(gitRef))
        {
            return repo.Head?.Tip;
        }

        // Try as branch name
        var branch = repo.Branches[gitRef];
        if (branch?.Tip != null)
            return branch.Tip;

        // Try as tag name
        var tag = repo.Tags[gitRef];
        if (tag != null)
        {
            var target = tag.Target;
            if (target is Commit tagCommit)
                return tagCommit;
            // Annotated tag — peel to commit
            if (target is TagAnnotation annotation)
                return annotation.Target as Commit;
        }

        // Try as commit SHA (full or partial)
        try
        {
            var obj = repo.Lookup(gitRef);
            if (obj is Commit c)
                return c;
        }
        catch
        {
            // Not a valid object id
        }

        return null;
    }

    private static void WriteZip(MemoryStream ms, Tree tree, string refName)
    {
        using var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true);
        AddTreeToZip(archive, tree, $"{refName}/");
    }

    private static void AddTreeToZip(ZipArchive archive, Tree tree, string basePath)
    {
        foreach (var entry in tree)
        {
            var entryPath = basePath + entry.Name;
            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                AddTreeToZip(archive, (Tree)entry.Target, entryPath + "/");
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                var blob = (Blob)entry.Target;
                var zipEntry = archive.CreateEntry(entryPath);
                using var blobStream = blob.GetContentStream();
                using var zipStream = zipEntry.Open();
                blobStream.CopyTo(zipStream);
            }
        }
    }

    private static void WriteTarGz(MemoryStream ms, Tree tree, string refName)
    {
        using var gzipStream = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true);
        using var tarWriter = new TarWriter(gzipStream, leaveOpen: true);
        AddTreeToTar(tarWriter, tree, $"{refName}/");
    }

    private static void AddTreeToTar(TarWriter tarWriter, Tree tree, string basePath)
    {
        foreach (var entry in tree)
        {
            var entryPath = basePath + entry.Name;
            if (entry.TargetType == TreeEntryTargetType.Tree)
            {
                AddTreeToTar(tarWriter, (Tree)entry.Target, entryPath + "/");
            }
            else if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                var blob = (Blob)entry.Target;
                using var blobStream = blob.GetContentStream();

                // Copy blob to a seekable MemoryStream for the tar entry
                var contentStream = new MemoryStream();
                blobStream.CopyTo(contentStream);
                contentStream.Position = 0;

                var tarEntry = new PaxTarEntry(TarEntryType.RegularFile, entryPath)
                {
                    DataStream = contentStream
                };
                tarWriter.WriteEntry(tarEntry);
            }
        }
    }
}
