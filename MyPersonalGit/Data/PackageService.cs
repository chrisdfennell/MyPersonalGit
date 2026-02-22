using Microsoft.EntityFrameworkCore;
using MyPersonalGit.Models;

namespace MyPersonalGit.Data;

public interface IPackageService
{
    Task<List<Package>> GetPackagesAsync(string? type = null, string? owner = null);
    Task<Package?> GetPackageAsync(string name, string type);
    Task<PackageVersion?> GetPackageVersionAsync(string name, string type, string version);
    Task<Package> CreateOrUpdatePackageAsync(string name, string type, string owner,
        string version, string? description = null, string? repoName = null, string? metadata = null);
    Task<PackageFile> AddPackageFileAsync(int packageVersionId, string filename, long size, string? sha256 = null);
    Task<bool> IncrementDownloadAsync(string name, string type, string version);
    Task<bool> DeletePackageVersionAsync(string name, string type, string version);
    Task<bool> DeletePackageAsync(string name, string type);
}

public class PackageService : IPackageService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<PackageService> _logger;

    public PackageService(IDbContextFactory<AppDbContext> dbFactory, ILogger<PackageService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<Package>> GetPackagesAsync(string? type = null, string? owner = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var query = db.Packages.Include(p => p.Versions).AsQueryable();
        if (!string.IsNullOrEmpty(type)) query = query.Where(p => p.Type == type);
        if (!string.IsNullOrEmpty(owner)) query = query.Where(p => p.Owner == owner);
        return await query.OrderByDescending(p => p.UpdatedAt).ToListAsync();
    }

    public async Task<Package?> GetPackageAsync(string name, string type)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Packages
            .Include(p => p.Versions).ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower() && p.Type == type);
    }

    public async Task<PackageVersion?> GetPackageVersionAsync(string name, string type, string version)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.PackageVersions
            .Include(v => v.Files)
            .FirstOrDefaultAsync(v =>
                v.Version == version &&
                db.Packages.Any(p => p.Id == v.PackageId && p.Name.ToLower() == name.ToLower() && p.Type == type));
    }

    public async Task<Package> CreateOrUpdatePackageAsync(string name, string type, string owner,
        string version, string? description = null, string? repoName = null, string? metadata = null)
    {
        using var db = _dbFactory.CreateDbContext();

        var pkg = await db.Packages.Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower() && p.Type == type);

        if (pkg == null)
        {
            pkg = new Package
            {
                Name = name,
                Type = type,
                Owner = owner,
                Description = description,
                RepositoryName = repoName
            };
            db.Packages.Add(pkg);
            await db.SaveChangesAsync();
        }

        if (description != null) pkg.Description = description;
        pkg.UpdatedAt = DateTime.UtcNow;

        var existingVersion = pkg.Versions.FirstOrDefault(v => v.Version == version);
        if (existingVersion == null)
        {
            db.PackageVersions.Add(new PackageVersion
            {
                PackageId = pkg.Id,
                Version = version,
                Description = description,
                Metadata = metadata
            });
        }
        else
        {
            existingVersion.Metadata = metadata ?? existingVersion.Metadata;
            existingVersion.Description = description ?? existingVersion.Description;
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Package {Type}/{Name}@{Version} published by {Owner}", type, name, version, owner);
        return pkg;
    }

    public async Task<PackageFile> AddPackageFileAsync(int packageVersionId, string filename, long size, string? sha256 = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var version = await db.PackageVersions.FindAsync(packageVersionId);
        if (version != null) version.Size += size;

        var file = new PackageFile
        {
            PackageVersionId = packageVersionId,
            Filename = filename,
            Size = size,
            Sha256 = sha256
        };
        db.PackageFiles.Add(file);
        await db.SaveChangesAsync();
        return file;
    }

    public async Task<bool> IncrementDownloadAsync(string name, string type, string version)
    {
        using var db = _dbFactory.CreateDbContext();
        var pkg = await db.Packages.FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower() && p.Type == type);
        var ver = await db.PackageVersions
            .FirstOrDefaultAsync(v => v.Version == version && v.PackageId == (pkg != null ? pkg.Id : 0));
        if (pkg == null || ver == null) return false;
        pkg.Downloads++;
        ver.Downloads++;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePackageVersionAsync(string name, string type, string version)
    {
        using var db = _dbFactory.CreateDbContext();
        var pkg = await db.Packages.FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower() && p.Type == type);
        if (pkg == null) return false;
        var ver = await db.PackageVersions.Include(v => v.Files)
            .FirstOrDefaultAsync(v => v.PackageId == pkg.Id && v.Version == version);
        if (ver == null) return false;
        db.PackageVersions.Remove(ver);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePackageAsync(string name, string type)
    {
        using var db = _dbFactory.CreateDbContext();
        var pkg = await db.Packages.Include(p => p.Versions).ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower() && p.Type == type);
        if (pkg == null) return false;
        db.Packages.Remove(pkg);
        await db.SaveChangesAsync();
        return true;
    }
}
