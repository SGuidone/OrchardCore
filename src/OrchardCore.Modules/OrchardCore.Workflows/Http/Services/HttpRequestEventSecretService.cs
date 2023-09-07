using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using OrchardCore.DisplayManagement;
using OrchardCore.Secrets;
using OrchardCore.Workflows.Http.Controllers;
using OrchardCore.Workflows.Http.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Workflows.Http.Services;

public class HttpRequestEventSecretService : IHttpRequestEventSecretService
{
    private const int NoExpiryTokenLifespan = HttpWorkflowController.NoExpiryTokenLifespan;
    private const string TokenCacheKeyPrefix = "HttpRequestEventToken:";

    private readonly IMemoryCache _memoryCache;
    private readonly ISecretService _secretService;
    private readonly ISecurityTokenService _securityTokenService;
    private readonly ViewContextAccessor _viewContextAccessor;
    private readonly IUrlHelperFactory _urlHelperFactory;

    public HttpRequestEventSecretService(
        IMemoryCache memoryCache,
        ISecretService secretService,
        ISecurityTokenService securityTokenService,
        ViewContextAccessor viewContextAccessor,
        IUrlHelperFactory urlHelperFactory)
    {
        _memoryCache = memoryCache;
        _secretService = secretService;
        _securityTokenService = securityTokenService;
        _viewContextAccessor = viewContextAccessor;
        _urlHelperFactory = urlHelperFactory;
    }

    public async Task<string> GetUrlAsync(string secretName)
    {
        var secret = await _secretService.GetSecretAsync<HttpRequestEventSecret>(secretName);
        if (secret is null || secret.WorkflowTypeId is null || secret.ActivityId is null)
        {
            return null;
        }

        // If the secret changes the key is no longer valid and the cache entry will expire automatically.
        var tokenLifeSpan = secret.TokenLifeSpan == 0 ? NoExpiryTokenLifespan : secret.TokenLifeSpan;
        var cacheKey = $"{TokenCacheKeyPrefix}{secret.WorkflowTypeId}{secret.ActivityId}{tokenLifeSpan}";

        var url = _memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(24);

            var urlHelper = _urlHelperFactory.GetUrlHelper(_viewContextAccessor.ViewContext);

            var token = _securityTokenService.CreateToken(
                new WorkflowPayload(secret.WorkflowTypeId, secret.ActivityId),
                TimeSpan.FromDays(tokenLifeSpan));

            return urlHelper.Action("Invoke", "HttpWorkflow", new { area = "OrchardCore.Workflows", token });
        });

        return url;
    }
}
