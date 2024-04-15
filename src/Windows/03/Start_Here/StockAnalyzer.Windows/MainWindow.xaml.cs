using Newtonsoft.Json;
using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace StockAnalyzer.Windows;

public partial class MainWindow : Window
{
    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private Stopwatch stopwatch = new Stopwatch();

    public MainWindow()
    {
        InitializeComponent();
    }

    async Task NestedAsync()
    {
        // Thread 1
        Task.Run(async () => // anonymous async method automatically returns a `Task`, not `async void`
        {
            // Thread 2
            await Task.Run(() =>
            {
                // Thread 3
            });
            // Thread 2
        });
        // Thread 1
    }

    async Task ErrorHandling()
    {
        var task = Task.Run(() => throw new Exception());
        
        try
        {
            await task;
        }
        catch (Exception e)
        {
            // Log e.Message
        }
        
        // Or

        await task.ContinueWith(t =>
        {
            // Log t.Exception.Message (aggregate exception)
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
    


    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        await ErrorHandling();
        
        try
        {
            BeforeLoadingStockData();

            var loadLinesTask = Task.Run(() => File.ReadAllLines("StockPrices_Small.csv"));

            // ContinueWith allows for continuation and it will run when task has finished, but
            // ContinueWith in contrast to await won't execute delegate on the original thread
            var processStocksTask = loadLinesTask.ContinueWith(completedTask =>
            {
                // Task has completed, so using Result is ok, it won't lock any thread it contains what the task returns
                var lines = completedTask.Result;
                var data = new List<StockPrice>();
                
                foreach (var line in lines.Skip(1))
                {
                    var price = StockPrice.FromCSV(line);
                    data.Add(price);
                }
                
                Dispatcher.Invoke(() => {
                    Stocks.ItemsSource = data.Where(sp => sp.Identifier == StockIdentifier.Text);
                });
            });
            
            processStocksTask.ContinueWith(_ => {
                Dispatcher.Invoke(AfterLoadingStockData);
            });
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
    }
    
    private async Task GetStocks()
    {
        try
        {
            var store = new DataStore();

            var responseTask = store.GetStockPrices(StockIdentifier.Text);

            Stocks.ItemsSource = await responseTask;
        }
        catch (Exception ex)
        {
            throw;
        }
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