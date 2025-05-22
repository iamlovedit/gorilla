using Gorilla.Domain.Services;
using Microsoft.AspNetCore.Mvc;
namespace GorillaBackend.Controllers;

[ApiController]
[Route("{username}/{repository}.git")]
public class GitActionController(IGitService gitService) : ControllerBase
{
    /// <summary>
    /// git-upload-pack (clone/fetch)
    /// </summary>
    [HttpPost("git-upload-pack")]
    public async Task<IActionResult> UploadPackAsync(string username, string repository)
    {
        var repoName = Path.Combine(username, repository);
        Response.ContentType = "application/x-git-upload-pack-result";
        var ok = await gitService.ExecuteGitCommandAsync(
            repoName,
            "upload-pack",
            "",
            Request.Body,
            Response.Body
        );
        if (!ok)
        {
            return NotFound();
        }

        return new EmptyResult();
    }

    /// <summary>
    /// git-receive-pack (push)
    /// </summary>
    [HttpPost("git-receive-pack")]
    public async Task<IActionResult> ReceivePackAsync(string username, string repository)
    {
        var repoName = Path.Combine(username, repository);
        Response.ContentType = "application/x-git-receive-pack-result";
        var ok = await gitService.ExecuteGitCommandAsync(
            repoName,
            "receive-pack",
            "",
            Request.Body,
            Response.Body
        );
        if (!ok)
        {
            return NotFound();
        }

        return new EmptyResult();
    }

    /// <summary>
    /// info/refs (clone/push handshake)
    /// </summary>
    [HttpGet("info/refs")]
    public async Task<IActionResult> GetInfoRefsAsync(string username, string repository, [FromQuery] string service)
    {
        if (string.IsNullOrEmpty(service))
            return BadRequest("Missing service parameter");
        var repoName = Path.Combine(username, repository);
        Response.ContentType = $"application/x-{service}-advertisement";
        var ok = await gitService.ExecuteGitAdvertisementCommandAsync(
            repoName,
            service,
            Response.Body
        );
        if (!ok)
        {
            return NotFound();
        }

        return new EmptyResult();
    }
}