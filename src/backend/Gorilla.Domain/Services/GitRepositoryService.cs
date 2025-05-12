using System.Diagnostics;
using LibGit2Sharp;
using Microsoft.Extensions.Configuration;

namespace Gorilla.Domain.Services;

public class GitRepositoryService(IConfiguration configuration) : IGitRepositoryService
{
    private readonly string _repositoryPath = configuration["RepositoryBasePath"] ??
                                              throw new NullReferenceException("repositoryBasePath is null");

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

    public async Task HandleInfoRefsAsync(string username, string repository, string service, Stream outputStream)
    {
        var repoPath = GetRepositoryPath(username, repository);
        await using var writer = new StreamWriter(outputStream, leaveOpen: true);
        // 写入服务头
        await writer.WriteAsync($"# service={service}\n");
        await writer.WriteAsync("0000"); // 以四个零结束头部
        await writer.FlushAsync();

        // 通常情况下，我们需要调用原生Git命令来获取正确的引用输出
        // LibGit2Sharp不直接支持Smart HTTP协议的所有方面
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"{service.Substring(4)} --stateless-rpc --advertise-refs \"{repoPath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start git process");
        }

        await process.StandardOutput.BaseStream.CopyToAsync(outputStream);
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command exited with code {process.ExitCode}");
        }
    }

    public async Task HandleUploadPackAsync(string username, string repository, Stream inputStream, Stream outputStream)
    {
        var repoPath = GetRepositoryPath(username, repository);

        // 处理git-upload-pack请求，使用原生Git命令
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"upload-pack --stateless-rpc \"{repoPath}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start git process");
        }

        var inputTask = inputStream.CopyToAsync(process.StandardInput.BaseStream);
        var outputTask = process.StandardOutput.BaseStream.CopyToAsync(outputStream);

        await inputTask;
        process.StandardInput.Close(); // 需要关闭输入流以通知Git进程输入结束

        await outputTask;
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command exited with code {process.ExitCode}");
        }
    }

    public async Task HandleReceivePackAsync(string username, string repository, Stream inputStream,
        Stream outputStream)
    {
        var repoPath = GetRepositoryPath(username, repository);
        // 处理git-receive-pack请求，使用原生Git命令
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"receive-pack --stateless-rpc \"{repoPath}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start git process");
        }

        var inputTask = inputStream.CopyToAsync(process.StandardInput.BaseStream);
        var outputTask = process.StandardOutput.BaseStream.CopyToAsync(outputStream);

        await inputTask;
        process.StandardInput.Close();

        await outputTask;
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command exited with code {process.ExitCode}");
        }
    }

    private string GetRepositoryPath(string username, string repository)
    {
        return Path.Combine(_repositoryPath, username, $"{repository}.git");
    }
}