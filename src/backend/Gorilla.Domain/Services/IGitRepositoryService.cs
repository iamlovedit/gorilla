using Gorilla.Domain.Models.Repositories;
using Microsoft.Extensions.Configuration;

namespace Gorilla.Domain.Services;

public interface IGitRepositoryService
{
    Task<Repository> CreateRepositoryAsync(string name, long ownerId, string description = "", bool isPrivate = false);
}

public class GitRepositoryService(IConfiguration configuration) : IGitRepositoryService
{
    public async Task<Repository> CreateRepositoryAsync(string name, long ownerId, string description = "",
        bool isPrivate = false)
    {
        throw new NotImplementedException();
    }
}