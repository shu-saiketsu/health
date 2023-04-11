﻿using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http;
using System.Net;

namespace Saiketsu.Health.WebApi.HealthChecks
{
    public sealed class ElectionHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ElectionHealthCheck(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            var client = _httpClientFactory.CreateClient("ElectionClient");

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
}