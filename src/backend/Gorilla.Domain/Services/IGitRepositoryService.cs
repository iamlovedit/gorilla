using Gorilla.Domain.Models.Repositories;

namespace Gorilla.Domain.Services;

public interface IGitRepositoryService
{
    void InitializeRepository(string username, string repository, bool isBare = true);

    bool RepositoryExistsAsync(string username, string repository);

    Task HandleInfoRefsAsync(string username, string repository, string service, Stream outputStream);

    Task HandleUploadPackAsync(string username, string repository, Stream inputStream, Stream outputStream);

    Task HandleReceivePackAsync(string username, string repository, Stream inputStream, Stream outputStream);
}