using Gorilla.Domain.Services;
using GorillaBackend.Middlewares;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.TryAddScoped<IGitRepositoryService, GitRepositoryService>();

services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.AddControllers();
services.AddAuthentication();
services.AddAuthorization();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<GitHttpMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();