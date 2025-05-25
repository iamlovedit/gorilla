using Microsoft.AspNetCore.Mvc;
using System.Text;
using Gorilla.Domain.Services.Repository;
using Microsoft.Extensions.Primitives;

namespace Gorilla.Repository.Controllers;

[ApiController]
[Route("{username}/{repository}.git")]
public class GitActionController(IGitService gitService) : ControllerBase
{
    private static bool ValidateUser(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Authorization", out var value))
        {
            return false;
        }

        var authHeader = value.ToString();
        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var encoded = authHeader.Substring("Basic ".Length).Trim();
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var parts = decoded.Split(':', 2);
            if (parts.Length != 2) return false;
            var username = parts[0];
            var password = parts[1];
            // 写死的账号密码
            return username == "user" && password == "password";
        }
        catch
        {
            return false;
        }
    }

    private IActionResult UnauthorizedWithAuthHeader()
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"Git Server\"";
        return Unauthorized();
    }

    private async Task<IActionResult> HandleGitCommand(string username, string repository, string command,
        string contentType)
    {
        if (!ValidateUser(Request))
        {
            return UnauthorizedWithAuthHeader();
        }

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
        return !ValidateUser(Request)
            ? Task.FromResult(UnauthorizedWithAuthHeader())
            : HandleGitCommand(username, repository, "upload-pack", "application/x-git-upload-pack-result");
    }

    /// <summary>
    /// git-receive-pack (push)
    /// </summary>
    [HttpPost("git-receive-pack")]
    public Task<IActionResult> ReceivePackAsync(string username, string repository)
    {
        return !ValidateUser(Request)
            ? Task.FromResult(UnauthorizedWithAuthHeader())
            : HandleGitCommand(username, repository, "receive-pack", "application/x-git-receive-pack-result");
    }

    /// <summary>
    /// info/refs (clone/push handshake)
    /// </summary>
    [HttpGet("info/refs")]
    public async Task<IActionResult> GetInfoRefsAsync(string username, string repository, [FromQuery] string service)
    {
        if (string.IsNullOrEmpty(service))
        {
            return BadRequest("Missing service parameter");
        }

        if (!ValidateUser(Request))
        {
            return UnauthorizedWithAuthHeader();
        }

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