using Gorilla.Domain.DataTransferObjects.Repositories;
using Gorilla.Domain.Services;
using LibGit2Sharp;
using Microsoft.AspNetCore.Mvc;

namespace GorillaBackend.Controllers;

[ApiController]
[Route("repository")]
public class RepositoryController(IGitRepositoryService gitRepositoryService) : ControllerBase
{
    [HttpPost("create")]
    public Task<IActionResult> CreateRepository([FromBody] CreateRepositoryDto repositoryDto)
    {
        gitRepositoryService.InitializeRepository("young", repositoryDto.Name);
        return Task.FromResult<IActionResult>(Ok("created done！"));
    }
}