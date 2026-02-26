using BusinessLogicLayer;
using DataAccessLayer.Models.DataBaseContext;
using GF3.WebApi.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.MapControllers();

app.Run();
