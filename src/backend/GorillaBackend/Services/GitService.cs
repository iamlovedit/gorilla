using System.Diagnostics;
using System.Text;
using Gorilla.Domain.Services;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GorillaBackend.Services;

public class GitService(IOptions<GitServerSettings> gitSettings, ILogger<GitService> logger) : IGitService
{
    private readonly string _repositoryPath = gitSettings.Value.RepositoryRootPath ??
                                              throw new NullReferenceException("repositoryBasePath is null");

    private readonly string _gitExePath = gitSettings.Value.GitExePath ??
                                          throw new NullReferenceException("git execute path is null");

    public void InitializeRepository(string username, string repository, bool isBare = true)
    {
        var repoPath = GetRepositoryPath(username, repository);
        Directory.CreateDirectory(Path.GetDirectoryName(repoPath) ?? throw new InvalidOperationException());
        Repository.Init(repoPath, isBare);
    }

    public bool RepositoryExistsAsync(string username, string repository)
    {
        var repoPath = GetRepositoryPath(username, repository);
        return Directory.Exists(repoPath) && Repository.IsValid(repoPath);
    }

    public async Task<bool> ExecuteGitCommandAsync(
        string repoName,
        string command,
        string args,
        Stream? requestBody,
        Stream responseBody)
    {
        var fullRepoPath = Path.Combine(_repositoryPath, repoName + ".git"); // 约定仓库名为 repoName.git
        if (!Directory.Exists(fullRepoPath))
        {
            logger.LogWarning($"Repository not found: {fullRepoPath}");
            return false; // 仓库不存在
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _gitExePath,
            Arguments = $"{command} --stateless-rpc \"{fullRepoPath}\"", // 关键：<directory>参数为仓库绝对路径
            WorkingDirectory = _repositoryPath, // 或 Path.GetDirectoryName(fullRepoPath)
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true // 不显示窗口
        };
        logger.LogInformation(
            $"Executing Git command: {startInfo.FileName} {startInfo.Arguments} in {startInfo.WorkingDirectory}");
        using var process = new Process();
        process.StartInfo = startInfo;
        try
        {
            process.Start();
            // 将HTTP请求体写入Git进程的标准输入
            if (requestBody is { CanRead: true })
            {
                await requestBody.CopyToAsync(process.StandardInput.BaseStream);
                process.StandardInput.Close(); // 关闭输入流，Smarter HTTP协议要求这样
            }

            // 将Git进程的标准输出直接写入HTTP响应体
            await process.StandardOutput.BaseStream.CopyToAsync(responseBody);
            // 等待进程结束
            await process.WaitForExitAsync();
            // 读取标准错误输出，用于调试和日志记录
            var errorOutput = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(errorOutput))
            {
                logger.LogError($"Git command stderr: {errorOutput}");
            }

            if (process.ExitCode == 0)
            {
                return true;
            }

            logger.LogError($"Git command exited with code {process.ExitCode} for path: {fullRepoPath}");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error executing Git command for repository {repoName}");
            return false;
        }
    }

    public async Task<bool> ExecuteGitAdvertisementCommandAsync(string repoName, string service, Stream responseBody)
    {
        var fullRepoPath = Path.Combine(_repositoryPath, repoName + ".git");
        if (!Directory.Exists(fullRepoPath))
        {
            logger.LogWarning($"Repository not found: {fullRepoPath}");
            return false;
        }

        // Write the Git protocol service header
        var serviceHeader = $"# service={service}\n";
        var headerBytes = Encoding.UTF8.GetBytes(serviceHeader);
        var headerPktLine = $"{(headerBytes.Length + 4).ToString("x4")}{serviceHeader}"; // 4 for length itself
        var headerPktLineBytes = Encoding.UTF8.GetBytes(headerPktLine);
        await responseBody.WriteAsync(headerPktLineBytes, 0, headerPktLineBytes.Length);
        // Write the FLUSH packet (0000)
        await responseBody.WriteAsync(Encoding.UTF8.GetBytes("0000"), 0, 4);
        var subcommand = service.Replace("git-", "");
        var startInfo = new ProcessStartInfo
        {
            FileName = _gitExePath,
            Arguments = $"{subcommand} --stateless-rpc --advertise-refs \"{fullRepoPath}\"", // 关键：加上仓库路径
            WorkingDirectory = _repositoryPath, // 或 Path.GetDirectoryName(fullRepoPath)
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        logger.LogInformation(
            $"Executing Git command for advertisement: {startInfo.FileName} {startInfo.Arguments} in {startInfo.WorkingDirectory}");
        using var process = new Process();
        process.StartInfo = startInfo;
        try
        {
            process.Start();
            // Close standard input immediately as there's no body for GET requests
            process.StandardInput.Close();
            // Stream Git process output directly to the response body
            await process.StandardOutput.BaseStream.CopyToAsync(responseBody);
            await process.WaitForExitAsync();
            var errorOutput = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(errorOutput))
            {
                logger.LogError($"Git command stderr: {errorOutput}");
            }

            if (process.ExitCode == 0)
            {
                return true;
            }

            logger.LogError($"Git command exited with code {process.ExitCode} for path: {fullRepoPath}");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error executing Git advertisement command for repository {repoName}");
            return false;
        }
    }


    private string GetRepositoryPath(string username, string repository)
    {
        return Path.Combine(_repositoryPath, username, $"{repository}.git");
    }
}

public class GitServerSettings
{
    public string GitExePath { get; set; }
    public string RepositoryRootPath { get; set; }
}