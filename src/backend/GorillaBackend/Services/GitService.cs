using System.Diagnostics;
using System.Text;
using GorillaBackend.Infrastructure;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GorillaBackend.Services;

public class GitService(IOptions<GitServerSettings> gitSettings, ILogger<GitService> logger) : IGitService
{
    private readonly string _repositoryPath = gitSettings.Value.RepositoryRootPath ??
                                              throw new NullReferenceException("repositoryBasePath is null");

    private readonly string _gitExePath = gitSettings.Value.GitExePath ??
                                          throw new NullReferenceException("git execute path is null");

    public void InitializeRepository(string username, string repository)
    {
        var repoPath = GetRepositoryPath(username, repository);
        Directory.CreateDirectory(Path.GetDirectoryName(repoPath) ?? throw new InvalidOperationException());
        Repository.Init(repoPath, true);
    }

    public bool RepositoryExistsAsync(string username, string repository)
    {
        var repoPath = GetRepositoryPath(username, repository);
        return Directory.Exists(repoPath) && Repository.IsValid(repoPath);
    }

    public async Task<bool> ExecuteGitCommandAsync(
        string repoName,
        string command,
        Stream? requestBody,
        Stream responseBody)
    {
        var fullRepoPath = Path.Combine(_repositoryPath, repoName + ".git");
        if (!Directory.Exists(fullRepoPath))
        {
            logger.LogWarning("Repository not found: {FullRepoPath}", fullRepoPath);
            return false;
        }
        var arguments = $"{command} --stateless-rpc \"{fullRepoPath}\"";
        return await RunGitProcessAsync(arguments, requestBody, responseBody, true, fullRepoPath, $"Git command for repository {repoName}");
    }

    public async Task<bool> ExecuteGitAdvertisementCommandAsync(string repoName, string service, Stream responseBody)
    {
        var fullRepoPath = Path.Combine(_repositoryPath, repoName + ".git");
        if (!Directory.Exists(fullRepoPath))
        {
            logger.LogWarning("Repository not found: {FullRepoPath}", fullRepoPath);
            return false;
        }
        // Write the Git protocol service header
        var serviceHeader = $"# service={service}\n";
        var headerBytes = Encoding.UTF8.GetBytes(serviceHeader);
        var headerPktLine = $"{headerBytes.Length + 4:x4}{serviceHeader}";
        var headerPktLineBytes = Encoding.UTF8.GetBytes(headerPktLine);
        await responseBody.WriteAsync(headerPktLineBytes);
        await responseBody.WriteAsync("0000"u8.ToArray().AsMemory(0, 4));
        var subcommand = service.Replace("git-", "");
        var arguments = $"{subcommand} --stateless-rpc --advertise-refs \"{fullRepoPath}\"";
        return await RunGitProcessAsync(arguments, null, responseBody, false, fullRepoPath, $"Git advertisement command for repository {repoName}");
    }

    /// <summary>
    /// 统一处理Git进程的启动、IO、日志和错误。
    /// </summary>
    private async Task<bool> RunGitProcessAsync(
        string arguments,
        Stream? requestBody,
        Stream responseBody,
        bool hasRequestBody,
        string fullRepoPath,
        string logContext)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _gitExePath,
            Arguments = arguments,
            WorkingDirectory = _repositoryPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        logger.LogInformation(
            "Executing {LogContext}: {FileName} {Arguments} in {WorkingDirectory}",
            logContext, startInfo.FileName, startInfo.Arguments, startInfo.WorkingDirectory);
        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
            if (hasRequestBody && requestBody is { CanRead: true })
            {
                await requestBody.CopyToAsync(process.StandardInput.BaseStream);
            }
            process.StandardInput.Close();
            await process.StandardOutput.BaseStream.CopyToAsync(responseBody);
            await process.WaitForExitAsync();
            var errorOutput = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(errorOutput))
            {
                logger.LogError("Git process stderr ({LogContext}): {ErrorOutput}", logContext, errorOutput);
            }
            if (process.ExitCode == 0)
            {
                return true;
            }
            logger.LogError("Git process exited with code {ExitCode} for {LogContext} at path: {FullRepoPath}", process.ExitCode, logContext, fullRepoPath);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing {LogContext} for path {FullRepoPath}", logContext, fullRepoPath);
            return false;
        }
    }

    private string GetRepositoryPath(string username, string repository)
    {
        return Path.Combine(_repositoryPath, username, $"{repository}.git");
    }
}