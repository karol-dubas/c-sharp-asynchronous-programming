using Newtonsoft.Json;
using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using StockAnalyzer.Core.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using StockAnalyzer.Windows.Services;

namespace StockAnalyzer.Windows;

public partial class MainWindow : Window
{
    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private Stopwatch stopwatch = new Stopwatch();

    public MainWindow()
    {
        InitializeComponent();
    }

    // private async void Search_Click(object sender, RoutedEventArgs e)
    // {
    //     try
    //     {
    //         BeforeLoadingStockData();
    //
    //         string[] identifiers = StockIdentifier.Text.Split(' ', ',');
    //         
    //         var data = new ObservableCollection<StockPrice>();
    //         Stocks.ItemsSource = data;
    //
    //         await using var service = new DiskStockStreamService();
    //         var enumerator = service.GetAllStockPrices();
    //
    //         await foreach (var stock in enumerator.WithCancellation(cts.Token))
    //         {
    //             if (identifiers.Contains(stock.Identifier))
    //                 data.Add(stock);
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Notes.Text = ex.Message;
    //     }
    //     finally
    //     {
    //         AfterLoadingStockData();
    //     }
    // }
    
    
    
    
    
    private async void Search_Click2(object sender, RoutedEventArgs e)
    {
        var data = await LoadStocks();
        Stocks.ItemsSource = data;
    }
    
    private void Search_Click(object sender, RoutedEventArgs e)
    {
        // var data = LoadStocks().Result;
        // Stocks.ItemsSource = data;

        var task = Task.Run(() =>
        {
            Dispatcher.Invoke(() => { });
        });
        
        task.Wait();
    }

    private async Task<IEnumerable<StockPrice>> LoadStocks()
    {
        var service = new StockService();
        var loadingTasks = new List<Task<IEnumerable<StockPrice>>>();

        foreach (string id in StockIdentifier.Text.Split(' ', ','))
        {
            var loadTask = service.GetStockPricesFor(id);
            loadingTasks.Add(loadTask);
        }
  
        var data = await Task.WhenAll(loadingTasks);
        return data.SelectMany(x => x);
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