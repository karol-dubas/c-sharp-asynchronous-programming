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
        if (cts is not null) // cancel scenario
        {
            cts.Cancel();
            cts = null;

            Search.Content = "Search";
            
            return;
        }

        try
        {
            cts = new CancellationTokenSource();

            Search.Content = "Cancel";

            BeforeLoadingStockData();

            var loadLinesTask = SearchForStocks();

            loadLinesTask.ContinueWith(
                t => { Dispatcher.Invoke(() => { Notes.Text = t.Exception.InnerException.Message; }); },
                TaskContinuationOptions.OnlyOnFaulted);

            var processStocksTask = loadLinesTask
                .ContinueWith(completedTask =>
                    {
                        var lines = completedTask.Result;

                        var data = new List<StockPrice>();

                        foreach (string line in lines.Skip(1))
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
                    cts = null;
                    Search.Content = "Search";
                });
            });
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
        finally
        {
            cts = null;
        }
    }

    private static Task<List<string>> SearchForStocks()
    {
        return Task.Run(async () =>
        {
            using var stream = new StreamReader(File.OpenRead("StockPrices_Small.csv"));
            var lines = new List<string>();

            string line;
            while ((line = await stream.ReadLineAsync()) != null)
                lines.Add(line);

            return lines;
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