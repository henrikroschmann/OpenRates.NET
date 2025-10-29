# OpenRates.NET

OpenRates.NET is an open-source .NET library and data service providing daily and historical currency exchange rates from trusted sources like the European Central Bank. Free, cacheable, and CDN-ready with built-in resilience patterns.

[![Build](https://github.com/henrikroschmann/OpenRates.NET/actions/workflows/build.yml/badge.svg)](https://github.com/henrikroschmann/OpenRates.NET/actions)
[![Tests](https://github.com/henrikroschmann/OpenRates.NET/actions/workflows/build.yml/badge.svg)](https://github.com/henrikroschmann/OpenRates.NET/actions)

[![NuGet](https://img.shields.io/nuget/v/OpenRates.Net.svg)](https://www.nuget.org/packages/OpenRates.Net/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Features

- 🚀 **High Performance**: Built-in hybrid caching with configurable expiration
- 🛡️ **Enterprise Ready**: Comprehensive error handling, logging, and cancellation support
- 📦 **Dependency Injection**: First-class support for Microsoft.Extensions.DependencyInjection
- 🔄 **Daily Updates**: Automated data refresh via GitHub Actions
- 🌐 **CDN Distribution**: Fast global access via jsDelivr CDN
- ✅ **Well Tested**: Comprehensive test coverage with xUnit

## Project Structure

This solution contains four projects:

- **OpenRates.Core**: Class library containing providers, models, and services
  - `Providers/EcbProvider.cs`: European Central Bank data provider with HttpClient injection
  - `Services/RateMerger.cs`: Merge rates from multiple sources with conflict resolution
  - `Models/ExchangeRates.cs`: Strongly-typed rate models
- **OpenRates.Publisher**: Console application for updating exchange rate data
  - Runs daily via GitHub Actions
  - Uses Microsoft.Extensions.Hosting for DI and logging
  - Includes graceful shutdown and error handling
- **OpenRates.Client**: NuGet SDK for accessing exchange rates
  - `HybridCache` integration for optimal performance
  - Automatic retry and error handling
  - Input validation and null safety
- **OpenRates.Tests**: xUnit test project with 8+ comprehensive tests

## Installation

### NuGet Package

Install the OpenRates.Net client SDK via NuGet:

```bash
dotnet add package OpenRates.Net
```

Or via Package Manager Console:

```powershell
Install-Package OpenRates.Net
```

**Package Details:**
- **Package ID**: `OpenRates.Net`
- **Version**: 1.0.0
- **Target Framework**: .NET 9.0
- **Dependencies**: 
  - Microsoft.Extensions.Caching.Hybrid (9.10.0)
  - OpenRates.Core (included)

**What's Included:**
- `IOpenRatesClient` interface for dependency injection
- `OpenRatesClient` implementation with built-in caching
- Full async/await support with cancellation tokens
- Comprehensive error handling and input validation

### Manual Build

If you want to build from source:

```bash
git clone https://github.com/henrikroschmann/OpenRates.NET.git
cd OpenRates.NET
dotnet restore
dotnet build
```

### Package Generation

To create a NuGet package locally:

```bash
cd src/OpenRates.Client
dotnet pack -c Release -o ./nupkg
```

## Quick Start

### Using the Client SDK

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenRates.Client;

var services = new ServiceCollection();

// Register OpenRates client with HttpClient factory and caching
services.AddHttpClient<IOpenRatesClient, OpenRatesClient>();
services.AddHybridCache();

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IOpenRatesClient>();

// Get current EUR to USD rate
var rate = await client.GetRateAsync("EUR", "USD");
Console.WriteLine($"EUR/USD: {rate}");

// Get historical rate
var historicalRate = await client.GetRateAsync(
    "EUR", 
    "GBP", 
    at: new DateTime(2024, 1, 15)
);
Console.WriteLine($"EUR/GBP on 2024-01-15: {historicalRate}");
```

### Using the Core Provider Directly

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenRates.Core.Providers;

var services = new ServiceCollection();
services.AddHttpClient<EcbProvider>();
services.AddLogging(builder => builder.AddConsole());

var provider = services.BuildServiceProvider();
var ecbProvider = provider.GetRequiredService<EcbProvider>();

var rates = await ecbProvider.FetchAsync(CancellationToken.None);
Console.WriteLine($"Fetched {rates.Rates.Count} base currencies");

// Get specific rate
var eurToUsd = rates.TryGet("EUR", "USD");
if (eurToUsd.HasValue)
{
    Console.WriteLine($"EUR/USD: {eurToUsd.Value}");
}
```

### Running the Publisher

```bash
cd src/OpenRates.Publisher
dotnet run

# Output:
# info: OpenRates.Core.Providers.EcbProvider[0]
#       Fetching ECB daily exchange rates from https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml
# info: OpenRates.Core.Providers.EcbProvider[0]
#       Successfully fetched 31 ECB exchange rates
# info: Program[0]
#       ✅ Rates updated: 2025-10-28
#       - Latest: data\latest.json
#       - Dated: data\2025-10-28.json
```

## Data Access

Exchange rate data is automatically updated daily and stored in the `/data` directory. The data is published to GitHub and can be accessed via jsDelivr CDN:

### CDN Endpoints

```
Latest rates:
https://cdn.jsdelivr.net/gh/henrikroschmann/OpenRates.NET@main/data/latest.json

Historical rates (by date):
https://cdn.jsdelivr.net/gh/henrikroschmann/OpenRates.NET@main/data/2025-10-28.json
```

### Data Format

```json
{
  "date": "2025-10-28T00:00:00",
  "rates": {
    "eur": {
      "usd": 1.0842,
      "gbp": 0.8567,
      "jpy": 162.45
    },
    "usd": {
      "eur": 0.9223
    }
  }
}
```

## Architecture

### Design Principles

- **SOLID**: Single responsibility, dependency inversion throughout
- **Async/Await**: Proper async patterns with cancellation token support
- **Null Safety**: Nullable reference types enabled across all projects
- **Error Handling**: Comprehensive try-catch with specific exception types
- **Resource Management**: HttpClient injection, no resource leaks
- **Logging**: Structured logging with Microsoft.Extensions.Logging
- **Caching**: Hybrid cache strategy for optimal performance

### Key Improvements

1. **No Resource Leaks**: HttpClient injected via IHttpClientFactory
2. **Comprehensive Error Handling**: All HTTP and parsing operations wrapped with specific exceptions
3. **Caching Restored**: HybridCache with 12-hour expiration prevents CDN hammering
4. **Input Validation**: ArgumentNullException and ArgumentException for all public APIs
5. **Cancellation Support**: CancellationToken throughout async operations
6. **Dependency Injection**: Full DI support with Microsoft.Extensions.Hosting
7. **Enhanced Testing**: 8 comprehensive tests with edge case coverage

## Building and Testing

### Build

```bash
dotnet restore
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Test Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

## Configuration

### Client Configuration

```csharp
services.AddHttpClient<IOpenRatesClient, OpenRatesClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromHours(12),
        LocalCacheExpiration = TimeSpan.FromHours(6)
    };
});
```

### Publisher Configuration

The publisher uses `appsettings.json` for configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

## Data Sources

- **European Central Bank (ECB)**: Daily reference rates for 30+ currencies against EUR
- **Future**: CoinGecko (cryptocurrency rates) - planned
- **Future**: Additional providers - planned

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow existing code style and patterns
- Add tests for new functionality
- Ensure all tests pass (`dotnet test`)
- Update documentation as needed
- Enable nullable reference types in new files

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Exchange rate data provided by the European Central Bank
- CDN hosting by jsDelivr
- Built with .NET 9.0

## Support

- 🐛 **Issues**: [GitHub Issues](https://github.com/henrikroschmann/OpenRates.NET/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/henrikroschmann/OpenRates.NET/discussions)

---

Made with ❤️ by the OpenRates.NET
