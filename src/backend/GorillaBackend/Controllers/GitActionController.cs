using Gorilla.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace GorillaBackend.Controllers;

[ApiController]
[Route("{username}/{repository}.git")]
public class GitActionController(IGitService gitService) : ControllerBase
{
    /// <summary>
    /// git push
    /// </summary>
    [HttpPost("git-upload-pack")]
    public async Task<IActionResult> UploadPackAsync(string username, string repository)
    {
        if (!Request.Headers.ContainsKey("Content-Type") || Request.Headers["Content-Type"] != "application/x-git-upload-pack-request")
        {
            return BadRequest("Invalid Content-Type for git-upload-pack. Expected: application/x-git-upload-pack-request");
        }
        Response.ContentType = "application/x-git-upload-pack-result";
        Response.Headers.CacheControl = "no-cache";
        var repoName = Path.Combine(username, repository);
        var success = await gitService.ExecuteGitCommandAsync(
            repoName,
            "upload-pack",
            "", // 数据传输阶段不需要advertise-refs
            Request.Body, // 将请求体作为Git进程的输入
            Response.Body); // 将Git进程的输出作为响应体
        if (!success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Git upload-pack command failed.");
        }
        return new EmptyResult();
    }

    /// <summary>
    /// git pull
    /// </summary>
    [HttpPost("git-receive-pack")]
    public IActionResult ReceivePackAsync(string username, string repository)
    {
        return Ok();
    }

    /// <summary>
    /// git clone 
    /// </summary>
    [HttpGet("info/refs")]
    public async Task<IActionResult> GetInfoRefsAsync(string username, string repository, string service)
    {
        if (string.IsNullOrEmpty(service))
        {
            return BadRequest("Service parameter is required.");
        }

        string gitCommand;
        string contentType;
        switch (service)
        {
            case "git-upload-pack":
                gitCommand = "upload-pack";
                contentType = "application/x-git-upload-pack-advertisement";
                break;
            case "git-receive-pack":
                gitCommand = "receive-pack";
                contentType = "application/x-git-receive-pack-advertisement";
                break;
            default:
                return BadRequest($"Unsupported service: {service}");
        }

        Response.ContentType = contentType;
        Response.Headers.CacheControl = "no-cache";
        var repoName = Path.Combine(username, repository);
        // 执行 git-upload-pack --stateless-rpc --advertise-refs 命令
        var success = await gitService.ExecuteGitCommandAsync(
            repoName,
            gitCommand,
            "--advertise-refs", // 发现阶段需要这个参数
            null, // GET请求没有请求体
            Response.Body);
        if (!success)
        {
            return NotFound($"Repository '{repoName}.git' not found or Git command failed.");
        }

        return new EmptyResult();
    }
}