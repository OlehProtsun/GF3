using Microsoft.AspNetCore.Http;
using WebApi.Infrastructure;

namespace GF3.Tests;

public class FrontendCachePolicyTests
{
    [Theory]
    [InlineData("/", "index.html", "no-store, max-age=0")]
    [InlineData("/index.html", "index.html", "no-store, max-age=0")]
    [InlineData("/employee/swap", "index.html", "no-store, max-age=0")]
    [InlineData("/assets/route", "index.html", "no-store, max-age=0")]
    [InlineData("/version.json", "version.json", "no-store, max-age=0")]
    [InlineData("/assets/index-abc123.js", "index-abc123.js", "public, max-age=31536000, immutable")]
    [InlineData("/manifest.json", "manifest.json", "no-cache, must-revalidate")]
    [InlineData("/icon.png", "icon.png", "no-cache, must-revalidate")]
    public void Served_file_controls_cache_policy(string path, string file, string expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        StartupConfiguration.ApplyFrontendCacheHeaders(context, file);
        Assert.Equal(expected, context.Response.Headers.CacheControl.ToString());
    }
}
