using Gorilla.Domain.DataTransferObjects.Repositories;
using Gorilla.Domain.Services;
using LibGit2Sharp;
using Microsoft.AspNetCore.Mvc;

namespace GorillaBackend.Controllers;

[ApiController]
[Route("repository")]
public class RepositoryController(IGitService gitService) : ControllerBase
{
    [HttpPost("create")]
    public Task<IActionResult> CreateRepository([FromBody] CreateRepositoryDto repositoryDto)
    {
        gitService.InitializeRepository("young", repositoryDto.Name,false);
        return Task.FromResult<IActionResult>(Ok("created done！"));
    }
}