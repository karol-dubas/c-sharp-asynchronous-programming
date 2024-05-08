using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using StockAnalyzer.Core.Domain;
using StockAnalyzer.Core.Services;

namespace StockAnalyzer.Windows;

public partial class MainWindow : Window
{
    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private readonly Stopwatch stopwatch = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Test_Click(object sender, RoutedEventArgs e)
    {
        var worker = new BackgroundWorker();
        
        worker.DoWork += (sender, e) =>
        {
            // Runs on a different thread
            Dispatcher.Invoke(() => Notes.Text += $"Worker DoWork{Environment.NewLine}");
        };
        
        worker.RunWorkerCompleted += (sender, e) =>
        {
            // Triggered when work is done  
            Notes.Text += $"Worker completed{Environment.NewLine}";
        };
        
        worker.RunWorkerAsync();
        
        // -------

        ThreadPool.QueueUserWorkItem(_ =>
        {
            // Run on a different thread
            Dispatcher.Invoke(() => Notes.Text += $"ThreadPool work item{Environment.NewLine}");
        });
    }
    
    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BeforeLoadingStockData();

            var data = await SearchForStocks();

            Stocks.ItemsSource = data.Where(price =>
                price.Identifier == StockIdentifier.Text);
        }
        catch(Exception ex)
        {
            Notes.Text = ex.Message;
        }
        finally
        {
            AfterLoadingStockData();
        }
    }

    private Task<IEnumerable<StockPrice>> SearchForStocks()
    {
        var tcs = new TaskCompletionSource<IEnumerable<StockPrice>>();
        
        ThreadPool.QueueUserWorkItem(_ => {
            var lines = File.ReadAllLines("StockPrices_Small.csv");
            var prices = new List<StockPrice>();

            foreach(var line in lines.Skip(1))
            {
                prices.Add(StockPrice.FromCSV(line));
            }

            // Sets Task status to RanToCompletion with result
            tcs.SetResult(prices);
        });

        return tcs.Task;
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