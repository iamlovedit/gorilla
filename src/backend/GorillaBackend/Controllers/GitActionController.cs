using Microsoft.AspNetCore.Mvc;

namespace GorillaBackend.Controllers;

[ApiController]
[Route("{username}/{repository}.git")]
public class GitActionController : ControllerBase
{
    /// <summary>
    /// git push
    /// </summary>
    [HttpGet("git-upload-pack")]
    public IActionResult UploadPackAsync(string username, string repository)
    {
        return Ok();
    }

    /// <summary>
    /// git pull
    /// </summary>
    [HttpGet("git-receive-pack")]
    public IActionResult ReceivePackAsync(string username, string repository)
    {
        return Ok();
    }

    /// <summary>
    /// git clone 
    /// </summary>
    [HttpGet("info/refs")]
    public IActionResult GetInfoRefsAsync(string username, string repository, string service)
    {
        return Ok();
    }
}