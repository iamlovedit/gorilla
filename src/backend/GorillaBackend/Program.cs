using Gorilla.Domain.Services;
using GorillaBackend.Infrastructure;
using GorillaBackend.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.TryAddScoped<IGitService, GitService>();

services.Configure<GitServerSettings>(builder.Configuration.GetSection("GitServerSettings"));

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
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();