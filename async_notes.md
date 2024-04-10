# 2.4 - Introducing async and await

## Asynchronous programming

### Overview
- `async` & `await` keywords
- Suitable for I/O operations
- Asynchronous operations occurs in parallel, but it subscribes to when that operation completes
- `Task` represents asynchronous operation
- `.Result` or `Wait()` on `Task` variable will block thread, it will run synchronously
- `await` waits for the operation to be completed, then continues execution. Pauses execution of the method until a result is available, without blocking the calling thread
- When debugging `await` call, while waiting for response control is returned to the calling thread 

### Usage examples:
- network resources
- disk write & read
- memory
- database

## Parrarel Programming

### Overview
- Task Parrarel Library (TPL)
- Suitable for CPU bound operations
- Split and solve small pieces independently, use as much computer resources as possible
- Allows for both asynchronous and parallel programming

# 2.5 - Understanding a continuation

- Asynchronous operations introduce separate threads where the work is being done
- `Task` represents asynchronous operation
- `await` keyword:
  - Retrieves result when available
  - Makes sure that there were no exceptions with awaited task
  - Introduces continuation that allows to get back to the original context (thread). 
     Code after `await` will run once task has completed and it will run on the same thread that spawned asynchronous operation
  - re-throws exceptions that occured inside the `Task` if task failed

```cs
    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        BeforeLoadingStockData();

        var getStocksTask = GetStocks();

        await getStocksTask;

        // Everything after await is a continuation

        AfterLoadingStockData();
    }

    private async Task GetStocks()
    {
        try
        {
            var store = new DataStore();

            var responseTask = store.GetStockPrices(StockIdentifier.Text);

            Stocks.ItemsSource = await responseTask; // Spawns async operation
        }
        catch (Exception ex) // Possible exception re-throw with await
        {
            Notes.Text = ex.Message;
        }
    }
```

# 2.6 - Creating own async method

- `async` keyword allows for using `await` keyword
- `async void` should be used only for event handlers
- `Task` represents an asynchoronous operation
- `async Task` method automatically returns `Task`, without explicit `return`. Compiler does it for us.
    ```cs
    public class Class 
    {
        public async Task Method() { }
    }
    ```

    is compiled to:

    ```cs
    // Other generated code...
    public Task Method()
    {
        <Method>d__0 stateMachine = new <Method>d__0();
        stateMachine.<>t__builder = AsyncTaskMethodBuilder.Create();
        stateMachine.<>4__this = this;
        stateMachine.<>1__state = -1;
        stateMachine.<>t__builder.Start(ref stateMachine);
        return stateMachine.<>t__builder.Task;
    }
    ```
- `Task` object returned from an asynchronous method is a reference to operation/result/error
```cs
var getStocksTask = GetStocks(); // Create separate thread with the code
await getStocksTask; // Execute code
```

