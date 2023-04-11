using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Saiketsu.Health.WebApi.HealthChecks;

public sealed class VoteHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;

    public VoteHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var client = _httpClientFactory.CreateClient("VoteClient");

        try
        {
            var response = await client.GetAsync(string.Empty, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK) return HealthCheckResult.Unhealthy();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return content switch
            {
                "Healthy" => HealthCheckResult.Healthy(),
                "Degraded" => HealthCheckResult.Degraded(),
                _ => HealthCheckResult.Unhealthy()
            };
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}