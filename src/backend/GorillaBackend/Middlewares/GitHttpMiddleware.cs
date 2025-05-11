namespace GorillaBackend.Middlewares;

public class GitHttpMiddleware(ILogger<GitHttpMiddleware> logger, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (!TryParseGitUrl(path, out var username, out var repository))
        {
            await next(context);
            return;
        }
        // 处理Git引用发现请求（这是git clone/pull/fetch的第一步）
        if (path.EndsWith("/info/refs") && context.Request.Query.ContainsKey("service"))
        {
            var service = context.Request.Query["service"].ToString();
            if (service is "git-upload-pack" or "git-receive-pack")
            {
                await HandleInfoRefs(context, username, repository, service);
                return;
            }
        }
        
    }

    private async Task HandleInfoRefs(HttpContext context, string username, string repository, string service)
    {
        // 设置Content-Type和其他必需的响应头
        context.Response.ContentType = $"application/x-{service}-advertisement";
        context.Response.Headers.Add("Cache-Control", "no-cache");
        
        try
        {
            // await _gitService.HandleInfoRefsAsync(username, repository, service, context.Response.Body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error handling info/refs for {service}");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }   
    
    private bool TryParseGitUrl(string path, out string username, out string repository)
    {
        username = string.Empty;
        repository = string.Empty;

        // format: /{username}/{repository}.git/...
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        username = parts[0];

        var repoWithSuffix = parts[1];
        if (repoWithSuffix.EndsWith(".git"))
        {
            repository = repoWithSuffix.Substring(0, repoWithSuffix.Length - 4);
            return true;
        }

        if ((parts.Length <= 2 || parts[2] != "info") && parts[2] != "git-upload-pack" &&
            parts[2] != "git-receive-pack")
        {
            return false;
        }

        repository = repoWithSuffix;
        return true;
    }
}