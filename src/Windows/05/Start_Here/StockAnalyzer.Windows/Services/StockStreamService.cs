using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using StockAnalyzer.Core.Domain;

namespace StockAnalyzer.Windows.Services;

public interface IStockStreamService
{
    IAsyncEnumerable<StockPrice>
        GetAllStockPrices(CancellationToken ct = default);
}

public class MockStockStreamService : IStockStreamService
{
    public async IAsyncEnumerable<StockPrice> GetAllStockPrices(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        string[] ids = { "MSFT", "GOOGL", "AAPL", "CAT" }; 
        
        for (int i = 0; i < 40; i++)
        {
            await Task.Delay(50, ct);
            yield return new StockPrice() { Identifier = ids[Random.Shared.Next(ids.Length)] };
        }
    }
}

public class DiskStockStreamService : IStockStreamService, IAsyncDisposable
{
    public async IAsyncEnumerable<StockPrice> GetAllStockPrices(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var stream = new StreamReader(File.Open("StockPrices_Small.csv", FileMode.Open));
        await stream.ReadLineAsync(ct);

        while (await stream.ReadLineAsync(ct) is { } line)
        {
            yield return StockPrice.FromCSV(line);
        }
    }

    public async ValueTask DisposeAsync()
    {
        // ... 
    }
}