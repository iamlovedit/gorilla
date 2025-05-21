using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace GorillaBackend.Infrastructure;

public class GitActionResult : IActionResult
{
    public Task ExecuteResultAsync(ActionContext context)
    {
        var httpBodyControlFeature = context.HttpContext.Features.Get<IHttpBodyControlFeature>();
        if (httpBodyControlFeature != null)
        {
            httpBodyControlFeature.AllowSynchronousIO = true;
        }

        var response = context.HttpContext.Response;
        return Task.CompletedTask;
    }
}