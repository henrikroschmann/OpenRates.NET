# OpenRates.NET
OpenRates.NET is an open-source .NET library and data service providing daily and historical currency exchange rates from trusted sources like the European Central Bank, ExchangeRate.host, and CoinGecko. Free, cacheable, and CDN-ready.

## Project Structure

This solution contains four projects:

- **OpenRates.Core**: Class library containing providers and data models
- **OpenRates.Publisher**: Console application for updating exchange rate data (runs via GitHub Actions)
- **OpenRates.Client**: NuGet SDK for accessing exchange rates
- **OpenRates.Tests**: xUnit test project

## Data Access

Exchange rate data is automatically updated daily and stored in the `/data` directory. The data is published to GitHub and can be accessed via jsDelivr CDN:

```
https://cdn.jsdelivr.net/gh/henrikroschmann/OpenRates.NET@main/data/<filename>.json
```

## Building and Testing

```bash
dotnet build
dotnet test
```
