using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using StockAnalyzer.Core.Domain;

namespace StockAnalyzer.Windows;

public partial class MainWindow : Window
{
    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private readonly Stopwatch stopwatch = new();
    private CancellationTokenSource? cts;
    
    public MainWindow()
    {
        InitializeComponent();
    }



    private void Search_Click(object sender, RoutedEventArgs e)
    {
        // Cancel loading
        if (cts is not null)
        {
            cts.Cancel();
            cts = null;
            Search.Content = "Search";
            return;
        }
        
        try
        {
            BeforeLoadingStockData();

            cts = new CancellationTokenSource();
            cts.Token.Register(() => Notes.Text = "Cancellation requested");
            Search.Content = "Cancel";

            var loadLinesTask = SearchForStocks(cts.Token);

            loadLinesTask.ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    Notes.Text = t.Exception.InnerException.Message;
                });

            }, TaskContinuationOptions.OnlyOnFaulted);

            var processStocksTask =
                loadLinesTask
                    .ContinueWith((completedTask) =>
                        {
                            var lines = completedTask.Result;

                            var data = new List<StockPrice>();

                            foreach (var line in lines.Skip(1))
                            {
                                var price = StockPrice.FromCSV(line);

                                data.Add(price);
                            }

                            Dispatcher.Invoke(() =>
                            {
                                Stocks.ItemsSource = data.Where(sp => sp.Identifier == StockIdentifier.Text);
                            });
                        },
                        TaskContinuationOptions.OnlyOnRanToCompletion
                    );

            processStocksTask.ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    AfterLoadingStockData();
                    
                    // Reset cts
                    cts = null;
                    Search.Content = "Search";
                });
            });
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
    }

    private static Task<List<string>> SearchForStocks(CancellationToken ct)
    {
        return Task.Run(async () => { 
            using(var stream = new StreamReader(File.OpenRead("StockPrices_Small.csv")))
            {
                var lines = new List<string>();

                string line;
                while((line = await stream.ReadLineAsync(ct)) != null)
                {
                    if (ct.IsCancellationRequested)
                        break;
                    lines.Add(line);
                }

                return lines;
            }
        });
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