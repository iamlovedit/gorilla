using Gorilla.Domain.Models.Repositories;

namespace Gorilla.Domain.Services;

public interface IGitService
{
    void InitializeRepository(string username, string repository, bool isBare = true);

    bool RepositoryExistsAsync(string username, string repository);

    Task<bool> ExecuteGitCommandAsync(string repoName, string command, string args, Stream? requestBody,
        Stream responseBody);
}