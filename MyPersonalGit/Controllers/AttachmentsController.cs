using Microsoft.AspNetCore.Mvc;
using MyPersonalGit.Data;

namespace MyPersonalGit.Controllers;

/// <summary>
/// Serves comment attachments (pasted/dropped images). Deliberately outside /api so
/// plain &lt;img&gt; tags can load them — access control is by possession of the
/// unguessable UUID in the URL, the same model GitHub uses for user-images.
/// </summary>
[ApiController]
public class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;

    public AttachmentsController(IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpGet("/attachments/{uuid}")]
    [HttpGet("/attachments/{uuid}/{fileName}")]
    public async Task<IActionResult> GetAttachment(string uuid, string? fileName = null)
    {
        var result = await _attachmentService.GetAttachmentAsync(uuid);
        if (result == null) return NotFound();

        var (attachment, path) = result.Value;

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "private, max-age=31536000, immutable";

        return PhysicalFile(path, attachment.ContentType);
    }
}
