using BusinessLogicLayer;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.AspNetCore.SpaServices.Extensions;
using Microsoft.EntityFrameworkCore;
using WebApi.Middleware;
using WebApi.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<AdminToolsOptions>(builder.Configuration.GetSection("AdminTools"));
builder.Services.PostConfigure<AdminToolsOptions>(options =>
{
    var enabled = Environment.GetEnvironmentVariable("GF3_ADMIN_ENABLED");
    if (bool.TryParse(enabled, out var enabledValue))
        options.Enabled = enabledValue;

    var token = Environment.GetEnvironmentVariable("GF3_ADMIN_TOKEN");
    if (!string.IsNullOrWhiteSpace(token))
        options.Token = token;

    var allowWrite = Environment.GetEnvironmentVariable("GF3_ADMIN_ALLOW_WRITE");
    if (bool.TryParse(allowWrite, out var allowWriteValue))
        options.AllowWriteSql = allowWriteValue;
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

if (app.Environment.IsDevelopment())
{
    app.UseCors("FrontendDev");
}

// ✅ Middleware краще вішати тільки на /api, щоб не чіпати SPA (/)
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), branch =>
{
    branch.UseMiddleware<ApiExceptionMiddleware>();
    branch.UseMiddleware<AdminToolsGuardMiddleware>();
});

app.MapControllers();

// ✅ DEV: React через Vite (проксі). Ти відкриваєш URL бекенда, а UI береться з Vite.
if (app.Environment.IsDevelopment())
{
    app.UseSpa(spa =>
    {
        // Папка React проекту відносно GF3.WebApi
        spa.Options.SourcePath = @"..\FrontEnd";

        // Порт Vite (зазвичай 5173)
        spa.UseProxyToSpaDevelopmentServer("http://localhost:5173");
    });
}
// ✅ PROD: віддавати build з wwwroot (коли зробиш npm run build і скопіюєш dist -> wwwroot)
else
{
    app.UseDefaultFiles();    // шукає index.html
    app.UseStaticFiles();     // віддає wwwroot/*
    app.MapFallbackToFile("index.html");
}

app.Run();