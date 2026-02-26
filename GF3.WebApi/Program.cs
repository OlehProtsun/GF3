using BusinessLogicLayer;
using DataAccessLayer.Models.DataBaseContext;
using WebApi.Middleware;
using Microsoft.EntityFrameworkCore;
using WebApi.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<AdminToolsOptions>(builder.Configuration.GetSection("AdminTools"));
builder.Services.PostConfigure<AdminToolsOptions>(options =>
{
    var enabled = Environment.GetEnvironmentVariable("GF3_ADMIN_ENABLED");
    if (bool.TryParse(enabled, out var enabledValue))
    {
        options.Enabled = enabledValue;
    }

    var token = Environment.GetEnvironmentVariable("GF3_ADMIN_TOKEN");
    if (!string.IsNullOrWhiteSpace(token))
    {
        options.Token = token;
    }

    var allowWrite = Environment.GetEnvironmentVariable("GF3_ADMIN_ALLOW_WRITE");
    if (bool.TryParse(allowWrite, out var allowWriteValue))
    {
        options.AllowWriteSql = allowWriteValue;
    }
});

var cs = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(cs))
{
    var root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GF3");

    Directory.CreateDirectory(root);

    var dbPath = Path.Combine(root, "SQLite.db");
    cs = $"Data Source={dbPath}";
}

builder.Services.AddBusinessLogicStack(cs);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseMiddleware<AdminToolsGuardMiddleware>();
app.MapControllers();

app.Run();
