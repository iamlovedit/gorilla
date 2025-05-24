namespace GorillaBackend.Services;

public interface IGitService
{
    void InitializeRepository(string username, string repository);

    bool RepositoryExistsAsync(string username, string repository);

    Task<bool> ExecuteGitCommandAsync(string repoName, string command, Stream? requestBody, Stream responseBody);

    Task<bool> ExecuteGitAdvertisementCommandAsync(string repoName, string service, Stream responseBody);
}