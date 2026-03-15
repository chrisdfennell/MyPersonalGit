using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

[ApiController]
public class ArtifactController : ControllerBase
{
    private readonly IArtifactService _artifactService;

    public ArtifactController(IArtifactService artifactService)
    {
        _artifactService = artifactService;
    }

    [HttpGet("api/v1/artifacts/{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var artifact = await _artifactService.GetArtifactAsync(id);
        if (artifact == null)
            return NotFound();

        if (!System.IO.File.Exists(artifact.FilePath))
            return NotFound("Artifact file not found on disk");

        var stream = System.IO.File.OpenRead(artifact.FilePath);
        return File(stream, "application/octet-stream", artifact.Name);
    }
}
