using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenRates.Core.Providers;
using OpenRates.Core.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure services
builder.Services.AddHttpClient<EcbProvider>();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var host = builder.Build();

try
{
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting OpenRates Publisher...");

    var ecbProvider = host.Services.GetRequiredService<EcbProvider>();
    var ecb = await ecbProvider.FetchAsync(cts.Token);
    var merged = RateMerger.Merge(ecb);

    // Ensure data folder exists
    Directory.CreateDirectory("data");

    var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var latestPath = Path.Combine("data", "latest.json");
    var datedPath = Path.Combine("data", $"{date}.json");

    JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    var json = JsonSerializer.Serialize(merged, jsonOptions);

    await File.WriteAllTextAsync(latestPath, json, cts.Token);
    await File.WriteAllTextAsync(datedPath, json, cts.Token);

    logger.LogInformation("✅ Rates updated: {Date}", date);
    logger.LogInformation("  - Latest: {Path}", latestPath);
    logger.LogInformation("  - Dated: {Path}", datedPath);

    return 0;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation cancelled by user.");
    return 1;
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Fatal error occurred while publishing rates");
    return 1;
}
