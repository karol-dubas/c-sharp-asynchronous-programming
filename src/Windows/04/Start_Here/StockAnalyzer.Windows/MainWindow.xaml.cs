using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using StockAnalyzer.Core.Services;

namespace StockAnalyzer.Windows;

public partial class MainWindow : Window
{
    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private readonly Stopwatch stopwatch = new();


    private CancellationTokenSource? cancellationTokenSource;

    public MainWindow()
    {
        InitializeComponent();
    }

    
    private async Task Method1()
    {
        // Thread 1
        await Method2();
        // Thread 1
    }
    
    private async Task Method2()
    {
        // Thread 1
        await Task.Run(() => { })
            .ConfigureAwait(false);
        // Thread 2
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        if (cancellationTokenSource != null)
        {
            // Already have an instance of the cancellation token source?
            // This means the button has already been pressed!

            cancellationTokenSource.Cancel();
            cancellationTokenSource = null;

            Search.Content = "Search";
            return;
        }

        try
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Token.Register(() => { Notes.Text = "Cancellation requested"; });
            Search.Content = "Cancel"; // Button text


            BeforeLoadingStockData();

            string[] ids = StockIdentifier.Text
                .Split(',', ' ');

            var service = new StockService();

            var loadingTasks = new List<Task>();
            var stocks = new ConcurrentBag<StockPrice>();

            foreach (string id in ids)
            {
                var loadTask = service.GetStockPricesFor(id, cancellationTokenSource.Token)
                    .ContinueWith(completedTask =>
                    {
                        foreach (var stock in completedTask.Result)
                        {
                            stocks.Add(stock);
                        }

                        UpdateStocksUi(stocks);
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);

                loadingTasks.Add(loadTask);
            }
            
            await Task.WhenAll(loadingTasks);
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
        finally
        {
            AfterLoadingStockData();

            cancellationTokenSource = null;
            Search.Content = "Search";
        }
    }

    private void UpdateStocksUi(ConcurrentBag<StockPrice> stocks)
    {
        Dispatcher.Invoke(() => { Stocks.ItemsSource = stocks.ToArray(); });
    }


    private static Task<List<string>> SearchForStocks(
        CancellationToken cancellationToken
    )
    {
        return Task.Run(async () =>
        {
            using var stream = new StreamReader(File.OpenRead("StockPrices_Small.csv"));

            var lines = new List<string>();

            while (await stream.ReadLineAsync() is string line)
            {
                if (cancellationToken.IsCancellationRequested) break;
                lines.Add(line);
            }

            return lines;
        }, cancellationToken);
    }

    private async Task GetStocks()
    {
        var store = new DataStore();

        var responseTask = store.GetStockPrices(StockIdentifier.Text);

        Stocks.ItemsSource = await responseTask;
    }


    private void BeforeLoadingStockData()
    {
        stopwatch.Restart();
        StockProgress.Visibility = Visibility.Visible;
        StockProgress.IsIndeterminate = true;
    }

    private void AfterLoadingStockData()
    {
        StocksStatus.Text = $"Loaded stocks for {StockIdentifier.Text} in {stopwatch.ElapsedMilliseconds}ms";
        StockProgress.Visibility = Visibility.Hidden;
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });

        e.Handled = true;
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}