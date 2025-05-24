using GorillaBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GorillaBackend.Controllers;

[ApiController]
[Route("{username}/{repository}.git")]
public class GitActionController(IGitService gitService) : ControllerBase
{
    private async Task<IActionResult> HandleGitCommand(string username, string repository, string command,
        string contentType)
    {
        var repoName = Path.Combine(username, repository);
        Response.ContentType = contentType;
        var ok = await gitService.ExecuteGitCommandAsync(
            repoName,
            command,
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
    /// git-upload-pack (clone/fetch)
    /// </summary>
    [HttpPost("git-upload-pack")]
    public Task<IActionResult> UploadPackAsync(string username, string repository)
    {
        return HandleGitCommand(username, repository, "upload-pack", "application/x-git-upload-pack-result");
    }

    /// <summary>
    /// git-receive-pack (push)
    /// </summary>
    [HttpPost("git-receive-pack")]
    public Task<IActionResult> ReceivePackAsync(string username, string repository)
    {
        return HandleGitCommand(username, repository, "receive-pack", "application/x-git-receive-pack-result");
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