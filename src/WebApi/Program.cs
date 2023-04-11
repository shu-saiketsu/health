using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Saiketsu.Health.WebApi.HealthChecks;
using Serilog;
using Serilog.Events;

const string serviceName = "Health";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", serviceName)
    .WriteTo.Console()
    .CreateBootstrapLogger();

static void InjectSerilog(WebApplicationBuilder builder)
{
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", serviceName)
        .WriteTo.Console());
}

static void AddHttpClients(WebApplicationBuilder builder)
{
    var services = new List<string> { "Gateway", "Party", "Candidate", "User", "Election", "Vote" };

    services.ForEach(service =>
    {
        var serviceName = $"{service}Client";
        var url = $"Services:{service}";

        builder.Services.AddHttpClient(serviceName, httpClient =>
        {
            var address = builder.Configuration[url];
            httpClient.BaseAddress = new Uri(address!);
        });
    });
}

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    InjectSerilog(builder);
    AddHttpClients(builder);

    builder.Services.AddHealthChecks()
        .AddCheck<CandidateHealthCheck>("Candidate")
        .AddCheck<ElectionHealthCheck>("Election")
        .AddCheck<GatewayHealthCheck>("Gateway")
        .AddCheck<PartyHealthCheck>("Party")
        .AddCheck<UserHealthCheck>("User")
        .AddCheck<VoteHealthCheck>("Vote");

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = (context, report) =>
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var options = new JsonWriterOptions { Indented = true };

            using var memoryStream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("status", report.Status.ToString());
                jsonWriter.WriteStartObject("results");

                foreach (var healthReportEntry in report.Entries)
                    jsonWriter.WriteString(healthReportEntry.Key,
                        healthReportEntry.Value.Status.ToString());

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            return context.Response.WriteAsync(
                Encoding.UTF8.GetString(memoryStream.ToArray()));
        }
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}