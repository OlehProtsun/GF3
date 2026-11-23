using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DataAccessLayer.Models;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Options;
using WebApi.Options;

namespace WebApi.Services;

public sealed class EmployeeWebPushSender(HttpClient httpClient, IOptions<PushOptions> options, TimeProvider timeProvider) : IEmployeeWebPushSender, IDisposable
{
    private readonly PushServiceClient client = new(httpClient) { AutoRetryAfter = false };
    public void Dispose() => httpClient.Dispose();
    public async Task SendAsync(EmployeePushSubscriptionModel subscription, EmployeePushCandidate candidate, CancellationToken ct)
    {
        var settings = options.Value;
        if (!settings.Enabled || !settings.AllowsEndpoint(subscription.Endpoint)) throw new InvalidOperationException("Push endpoint is not permitted.");
        if (!(candidate.Url is "/schedule" or "/availability" || Regex.IsMatch(candidate.Url, @"^/swap\?swapId=[1-9][0-9]*$")))
            throw new InvalidOperationException("Push route is not permitted.");
        var payload = JsonSerializer.Serialize(new
        {
            notificationId = candidate.NotificationId, title = candidate.Title, body = candidate.Body, url = candidate.Url, tone = candidate.Tone
        });
        if (Encoding.UTF8.GetByteCount(payload) > 3072) throw new InvalidOperationException("Push payload exceeds the limit.");
        var remaining = candidate.OccurredAtUtc + EmployeeNotificationRetention.Duration - timeProvider.GetUtcNow();
        var ttl = Math.Clamp((int)remaining.TotalSeconds, 0, candidate.NotificationId.StartsWith("swap-public:", StringComparison.Ordinal) || candidate.NotificationId.StartsWith("open-shift:", StringComparison.Ordinal) ? 3600 : 432000);
        var target = new PushSubscription
        {
            Endpoint = subscription.Endpoint,
            Keys = new Dictionary<string, string> { ["p256dh"] = subscription.P256dh, ["auth"] = subscription.Auth },
        };
        using var authentication = new VapidAuthentication(settings.VapidPublicKey, settings.VapidPrivateKey)
        {
            Subject = settings.VapidSubject,
        };
        await client.RequestPushMessageDeliveryAsync(target, new PushMessage(payload) { TimeToLive = ttl }, authentication, ct);
    }
}
