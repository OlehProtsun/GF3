using System.Net.WebSockets;
using System.Text;
using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Security;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Abstractions;
using GF3.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Options;
using WebApi.Realtime;

namespace GF3.Tests;

public sealed class RealtimeAuthenticationExpiryTests
{
    [Fact]
    public async Task PresenceWebSocket_ClosesWhenSignedTokenExpires_AndCannotReconnect()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var manager = new ManagerAccountModel
        {
            Username = "expiry.manager", DisplayName = "Expiry Manager", PasswordHash = "unused",
            PasswordUpdatedAtUtc = DateTimeOffset.UtcNow, CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.ManagerAccounts.Add(manager);
        await db.SaveChangesAsync();
        var jwtOptions = new JwtAuthOptions { Issuer = "tests", Audience = "tests", SigningKey = "realtime-expiry-test-signing-key-2026" };
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(database.ConnectionString));
        builder.Services.AddScoped<IManagerAccountRepository, ManagerAccountRepository>();
        builder.Services.AddScoped<IEmployeeAccountRepository, EmployeeAccountRepository>();
        builder.Services.AddScoped<IEmployeeAccountService, EmployeeAccountService>();
        builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
        builder.Services.AddSingleton<IEmployeePresenceService, EmployeePresenceService>();
        builder.Services.AddSingleton<IManagerPresenceService, ManagerPresenceService>();
        builder.Services.AddSingleton<IManagerEditLockService, ManagerEditLockService>();
        builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();
        builder.Services.AddSingleton(Options.Create(jwtOptions));
        builder.Services.AddAuthentication(JwtAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, JwtAuthenticationHandler>(JwtAuthenticationDefaults.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSignalR();
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHub<EmployeePresenceHub>(EmployeePresenceHub.RoutePattern, options => options.CloseOnAuthenticationExpiration = true);
        await app.StartAsync();
        try
        {
            var expires = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddSeconds(4).ToUnixTimeSeconds());
            var token = JwtTokenCodec.WriteAccessToken(jwtOptions, new AuthenticatedSessionDto
            {
                Role = AuthRoles.Manager, UserName = manager.Username, ManagerId = manager.Id,
                CredentialVersion = manager.PasswordUpdatedAtUtc.UtcTicks,
            }, DateTimeOffset.UtcNow, expires);
            var address = new Uri(app.Urls.Single().Replace("http://", "ws://") + EmployeePresenceHub.RoutePattern);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var socket = new ClientWebSocket();
            socket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
            await socket.ConnectAsync(address, timeout.Token);
            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"protocol\":\"json\",\"version\":1}\u001e")), WebSocketMessageType.Text, true, timeout.Token);
            var buffer = new byte[4096];
            var messages = new StringBuilder();
            while (true)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
                if (result.MessageType == WebSocketMessageType.Close) break;
                messages.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            Assert.Contains("{}\u001e", messages.ToString());
            Assert.True(DateTimeOffset.UtcNow >= expires);
            using var reconnect = new ClientWebSocket();
            reconnect.Options.SetRequestHeader("Authorization", $"Bearer {token}");
            await Assert.ThrowsAsync<WebSocketException>(() => reconnect.ConnectAsync(address, timeout.Token));
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
