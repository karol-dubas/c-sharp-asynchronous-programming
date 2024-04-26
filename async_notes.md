# 2.4 - Introducing async and await

## Asynchronous programming

### Overview
- `async` & `await` keywords
- Suitable for I/O operations
- Asynchronous operations occurs in parallel, but it subscribes to when that operation completes
- `Task` represents asynchronous operation
- `await` waits for the operation to be completed, then continues execution. Pauses execution of the method until a result is available, without blocking the calling thread
- When debugging `await` call, while waiting for response control is returned to the calling thread (e.g. UI)

### Usage examples:
- network resources
- disk write & read
- memory
- database

## Parrarel Programming
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
  - Re-throws exceptions that occured inside the `Task`, if task failed

  ```cs
  async void Search_Click(...)
  {
      var getStocksTask = GetStocks();
      await getStocksTask; // async/await all the way up
  
      // Everything after await is a continuation
  
      AfterLoadingStockData();
  }
  
  async Task GetStocks()
  {
      try
      {
          var store = new DataStore();
          Task responseTask = store.GetStockPrices();
          Stocks.ItemsSource = await responseTask; // Spawns async operation
      }
      // If responseTask throws an exception (on execution), then it will be re-throwed (await) and caught here
      catch (Exception ex)
      {
          Notes.Text = ex.Message;
      }
  }
  ```
  - Can be used only with method that returns `Task`
    
    ```cs
    async Task AsyncTaskMethod()
    {
        await TaskMethod();
    }

    Task TaskMethod() // no async
    {
        // ...
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
var getStocksTask = GetStocks(); // Create separate thread, with the code to execute
await getStocksTask; // Execute this code
```

# 2.7 - Handling exceptions

## Missing await

- Re-throwing exceptions sets the `Task` to faulted with an exception
- Without `await`, exception isn't re-throwed
  
  ```cs
  async void Search_Click(...)
  {
      try
      {
          /*await*/ GetStocks();
  
          // Execution isn't awaited, so it continues before the call is completed
          // and we have no idea what happened to this task
      }
      catch (Exception ex) // No await = no catch
      {
          Notes.Text = ex.Message;
      }
  }
  
  async Task GetStocks()
  {
      throw new Exception("I love exception mechanism <3");
      // Task status is set to Faulted
  }
  ```

## async void

- If `async` method returns a `Task` then we can use all the additional info, e.g. exceptions
  (`Task` is automatically returned, when method signature indicates it).
  But when it returns `void` there is no additional info (and call can't be awaited)
- It may crash the application when there is an unhandled exception. 
  Exceptions occuring in `async void` can't be caught

  ```cs
  void Search_Click(...)
  {
      try
      {
          GetStocks();
      }
      catch (Exception ex)
      {
          Notes.Text = ex.Message;
      }
  }  

  async void GetStocks() // async void method
  {
      try
      {
          // ...
          Stocks.ItemsSource = await responseTask; // Exception thrown here
      }
      catch (Exception ex) // Demo try catch, it's useless (lost stack trace)
      {
          // The return type is void, not a Task, exception can't be set on a void,
          // so it's thrown back to the caller and app crashes.
          // Returning an exception in Task would be correct (compiler does it automatically).
          throw ex;
      }
  }
  ```
  
  - When working with `async void` whole code in method should be in `try`, `catch`, `finally` blocks,
    without `throw`, so it makes sure no exception is thrown back to the caller (prevents app crash)

# 2.8 - Best practices

- Asynchronous ASP.NET relieves web server of work and it can take care of other requests while asynchronous
  operations are running
- `.Result` or `Wait` on `Task` variable will block thread, execution will run synchronously,
  it may even deadlock whole application
- Using `.Result` in the continuation is fine

  ```cs
  async void GetStocks()
  {
      var store = new DataStore();
      var responseTask = store.GetStockPrices("MSFT");
      await responseTask;

      Stocks.ItemsSource = responseTask.Result; // it was awaited, so Result is fine here
  }
  ```

# 3.1 - TPL

## Task class

- `Task` allows:
  - to execute work on a different thread
  - to get the result from asynchronous operation
  - to subscribe to when operation is done + continuation
  - for exception handling

## Task.Run static method

`Task.Run` queues methods on the thread pool for execution.

```cs
Task task1 = Task.Run(() => { /* Heavy operation */ });

// Generic version with return value
Task task2 = Task.Run<T>(() => { return new T(); });
```

# 3.1 - Task intro

```cs
// Offloading work on another thread.
// It queues this anonymous method execution on the thread pool
// and it shoud be executed immediately (assuming that thread pool isn't busy).
var data = await Task.Run(() =>
{
    // Assuming that ReadAllLinesAsync version isn't available
    var lines = File.ReadAllLines("file.csv");
    var data = new List<StockPrice>();
    
    foreach (var line in lines)
    {
        var price = StockPrice.FromCSV(line);
        data.Add(price);
    }
    
    return data;
});

Stocks.ItemsSource = data.Where(sp => sp.Identifier == StockIdentifier.Text);
```

# 3.2 - Creating async operation with TPL Task

- When not awaiting `Task.Run`, the task scheduler will queue this to the thread pool
  and it will execute that whenever there is an available thread.
  This call will complete immediately and code execution will continue (no await)

```cs
Task.Run(() => {});
```

# 3.3 - Obtaining the async result without async await keywords

- `ContinueWith` is an alternative approach to subscribe to an async operation

```cs
void Search_Click(...)
{
    try
    {
        var loadLinesTask = Task.Run(() => File.ReadAllLines("StockPrices_Small.csv"));

        // ContinueWith allows for continuation and it will run when task has finished
        var processStocksTask = loadLinesTask.ContinueWith(completedTask =>
        {
            // In contrast to await continuation, it won't execute on the original thread.
            
            // Task has completed, so using Result is ok here,
            // it won't lock any thread, it just contains what the task returns
            var lines = completedTask.Result;
        });
    }
    catch (Exception ex)
    {
        Notes.Text = ex.Message;
    }
}
```
- `async` & `await` is easier to read, has less code and is less error prone

# 3.4 - Nested asynchronous operations

```cs
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
        // Thread 2 (continuation)
    });
    // Thread 1
}
```

# 3.5 - Handling Task success and failure

- No matter how async operation is executed, it should always be validated (exception handling).
  To validate task execution either use:
  - `async`, `await` and `try`, `catch`
    
  ```cs
  try
  {
    // ...
      await task;
  }
  catch (Exception e)
  {
      // Log e.Message
  }
  ```
  - `ContinueWith` with error handling options
    
  ```cs
  await task.ContinueWith(t =>
  {
      // Log t.Exception.Message (aggregate exception)
  }, TaskContinuationOptions.OnlyOnFaulted);
  ```

- Working with `ContinueWith`:
  
```cs
var loadLinesTask = Task.Run(() => throw new FileNotFoundException());

loadLinesTask.ContinueWith(completedTask => { /* Will always run */ });

loadLinesTask.ContinueWith(completedTask =>
{
    // Will run if successfully completed
}, TaskContinuationOptions.OnlyOnRanToCompletion);
```
# 3.6 - Canceling a Task with CancellationTokenSource

```cs
CancellationTokenSource cts = new();
CancellationToken ct = cts.Token;

// CancellationToken can be passed to async methods
```

- `CancellationTokenSource`:
  - Provides cancellation request mechanism using cancellation tokens
  - Signals to a `CancellationToken` that it should be canceled with `Cancel`/`CancelAfter` methods
  - Calling `CancellationTokenSource.Cancel` won't automatically cancel asynchronous operations,
    `CancellationToken` and its members are used for that purpose
  - It is created at the source of an operation/s that may need to be cancelled
  
- `CancellationToken`:
  - Obtained as a property from `CancellationTokenSource`
  - Indicates to a task that it's canceled, represents cancellation itself,
    which can be checked or received to handle cancellation request.
    Doesn't have the capability to trigger cancellation, it can be only observed
  - It's passed to various methods that support cancellation, to observe and react to cancellation request

- Passing `CancellationToken` to `Task.Run` won't stop execution "inside",
  it just won't start `Task.Run` if `CancellationToken` is marked as cancelled
  
```cs
cts.Cancel();
var task = Task.Run(() => { }, ct); // won't start, doens't affect anonymous method
task.ContinueWith(t => { }, ct); // same logic applies here
```

- `TaskStatus` has different values depending on how we chain continuation methods:

```cs
var ct = new CancellationToken(canceled: true);
var task = Task.Run(() => "I won't even start", ct);

task.ContinueWith(t => {
    // t.Status = TaskStatus.Canceled
});

task.ContinueWith(t => {
    // t.Status = TaskStatus.Canceled
});
```
but chaining next `Task` with `ContinueWith` is different:

```cs
var ct = new CancellationToken(canceled: true);
var task = Task.Run(() => "I won't even start", ct);

task.ContinueWith(t => {
    // t.Status = TaskStatus.Canceled
})
.ContinueWith(t => {
    // t.Status = TaskStatus.RanToCompletion
});
```

- To cancel own long running task we can use `CancellationToken.IsCancellationRequested`:
  
```cs
Task.Run(() =>
{
    while (true)
    {
        if (ct.IsCancellationRequested)
            break;
    }
});
```
- Execution of operation can be registered on operation cancellation
  
```cs
cts.Token.Register(() => Notes.Text = "Cancellation requested");
```

# 3.7 - HTTP Client cancellation

- Canceling defined asynchronous methods might be different.
  Sometimes it might return partial data and sometimes it throws `TaskCanceledException` to let know that it was canceled

```cs
using var httpClient = new HttpClient();
var result = await httpClient.GetAsync(url, ct);
```

# 3.8 - Summary

- If two methods are needed, sync and async version, don't wrap sync version with `Task.Run` just to make it async.
  It should be copied and refactored, implemented properly.

# 4.2 - Knowing When All or Any Task Completes

```cs
foreach (string identifier in identifiers)
{
    // Each call wii be awaited one by one (await)
    var loadTask = await service.GetStockPricesFor(identifier, cancellationTokenSource.Token);
}
```

- Data can be loaded in parallel by performing multiple asynchronous operations at the same time
- `Task.WhenAll` accepts a task collection, it creates and returns a `Task`.
  Returned task status is completed only when all the tasks passed to the method are marked as completed.

  ```cs
  var loadingTasks = new List<Task<IEnumerable<int>>>();

  foreach (string id in ids)
  {
      var loadTask = service.GetAsync(id, cts.Token);
      loadingTasks.Add(loadTask);
  }
  
  var allResults = await Task.WhenAll(loadingTasks);
  ```

- With no `await` it isn't executed, it's a representation of loading all the stocks

  ```cs
  var allResults = Task.WhenAll(loadingTasks);
  ```
- `Task.WhenAny` returns `Task` after the completion of any first task.
  It can be used to create a timeout:

```cs
// ...

var timeoutTask = Task.Delay(2000); // timeout after 2s
var loadAllStocksAtOnceTask = Task.WhenAll(loadingTasks);

var firstCompletedTask = await Task.WhenAny(loadAllStocksAtOnceTask, timeoutTask);

if (firstCompletedTask == timeoutTask)
    throw new OperationCanceledException("Loading timeout");

return loadAllStocksAtOnceTask.Result;
```

but it's easier to use `cancellationTokenSource.CancelAfter` method to achieve a timeout

- When `Task.WhenAll/WhenAny` are awaited it ensures that if any task failed within method, the exception will be propagated back to the calling context

# 4.3 - Precomputed Results of a Task

- Precomputed results of a `Task` are used to return results from methods where we don't want to use `async` & `await`, or `Task.Run`, so we don't run a new task.
- Adding `async` & `await` when they're not needed introduces a whole unnecessary async state machine and is just more complex
- When implementing interface / overriding a method that forces to return a `Task`, but the implementation doesn't have any asynchronous operation, a `Task.CompletedTask` can be returned instead

```cs
public Task Method() // no async
{
    return Task.CompletedTask;
    // Everything completed successfuly, with no method signature change
}

// Async implementation would be awaited
await Method(); // completes immediately
```

- Methods `Task.FromResult`, `Task.FromCanceled`, `Task.FromException` can be used like `Task.CompletedTask`, but with a return value. 
- `Task.FromResult` creates a `Task` which is marked as completed with the specified result, so it's just a `Task` result wrapper

# 4.4. Process Tasks as They Complete

- Standard .NET collections aren't thread-safe.
  Thread-safe collections should be used when working with collections on multiple threads
- Data can be processed on the fly as subsequent tasks are completed

```cs
var loadingTasks = new List<Task>();
var stocks = new ConcurrentBag<StockPrice>();

foreach (string id in ids)
{
    var loadTask = service.GetStockPricesFor(id, cts.Token)
        .ContinueWith(completedTask =>
        {
            foreach (var stock in completedTask.Result)
                stocks.Add(stock);
            
            UpdateStocksUi(stocks);
        }, 
        TaskContinuationOptions.OnlyOnRanToCompletion);

    loadingTasks.Add(loadTask);
}

await Task.WhenAll(loadingTasks);
```

# 4.5. Execution Context and Controlling the Continuation

- For reasons like performance or context switching, there isn't always a need to return to the original context,
  when running the code in the continuations
- `Task.ConfigureAwait`:
  - Configures method how the continuation should execute.
  - It can be configured with `ConfigureAwait(false)` to continue on the new thread.
    Each method marked with `async` has its own asynchronous context, therefore
    it only affects the continuation in the method that you are operating in.
  - No code after `ConfigureAwait(false)` should require the original context.
  - `ConfigureAwait(false)` means, that there is no continuation enqueue on a thread pool.
    It's quicker than waiting for another thread to be available.

```cs
private async Task Method1()
{
    // Thread 1
    await Method2();
    // Thread 1 (marshaled back)
}

private async Task Method2()
{
    // Thread 1
    await Task.Run(() => { })
        .ConfigureAwait(false);
    // Thread 2
    // Continue on new thread
}
```

# 4.6. ConfigureAwait in ASP.NET

- ASP.NET Core doesn't use synchronization context, thus making `ConfigureAwait(false)` is useless
- `ConfigureAwait(false)` should be used in libraries, because the library can be used by any type of application

# 5.2. Asynchronous Streams and Disposables

- Allows for asynchronous retrieval of each item
- `IAsyncEnumerable<T>` exposes an enumerator that provides asynchronous iteration
- No need to return `Task` if method is `async`
- Method must `yield return`
- `async` + `IAsyncEnumerable<T>` = asynchronous enumeration
- Using `yield return` with `IAsyncEnumerable<T>` signals to the iterator using this enumerator that it has an item to process
- `IAsyncEnumerable<T>` can't be awaited, because it's an enumeration
- `await foreach`: 
  - Is used to asynchronously retrieve the data
  - It awaits each item in the enumeration
  - `foreach` body then is a continuation of each enumeration

- Producing a stream:

```cs
public async IAsyncEnumerable<StockPrice> GetAllStockPrices(CancellationToken ct = default)
{
    await Task.Delay(50, ct); // fake delay
    yield return new StockPrice() { Identifier = "TEST" };
}
```

- Consuming a stream:

```cs
await foreach (var stock in enumerator)
{

}
```

================================================================================================

# Questions / TODO

1. What is the difference?
   
```cs
Task.Run(() => GetStocks());
Task.Run(async () => await GetStocks());

async Task GetStocks() { /* ... */ }
```

1. When `Task.Run` is executed? What is the difference?

```cs
var loadLinesTask = SearchForStocks();

Task<List<string>> SearchForStocks()
{
    return Task.Run(async () =>
    {
        // ...
        return lines;
    });
}
```

```cs
var loadLinesTask = awaitSearchForStocks();

async Task<List<string>> SearchForStocks()
{
    return await Task.Run(async () =>
    {
        // ...
        return lines;
    });
}
```

1. What is the point of that?
   
```cs
async Task AsyncTaskMethod()
{
    await TaskMethod();
}

Task TaskMethod() // no async
{
    // ...
}
```

1. Can I spawn too many threads and overflow a thread pool?

1. When is the method executed? What if an exception is thrown?

```cs
void Search_Click(...)
{
    try
    {
        var loadLinesTask = Task.Run(() => File.ReadAllLines("StockPrices_Small.csv"));
        var processStocksTask = loadLinesTask.ContinueWith(completedTask => { /* continuation */ });
    }
    catch (Exception ex)
    {
        Notes.Text = ex.Message;
    }
}
```

1. Try different `ContinueWith` chain executions, `OnlyOnFaulted` etc.

1. Multiple awaits and exception.
   If `responseTask` throws an exception (on execution), then it will be re-throwed (await) and caught, but what about `getStocksTask`?

  ```cs
  async void Search_Click(...)
  {
      var getStocksTask = GetStocks();
      await getStocksTask;
      AfterLoadingStockData();
  }
  
  async Task GetStocks()
  {
      try
      {
          var store = new DataStore();
          Task responseTask = store.GetStockPrices();
          Stocks.ItemsSource = await responseTask;
      }
      catch (Exception ex)
      {
          Notes.Text = ex.Message;
      }
  }
  ```

1. Standard .NET collections aren't thread-safe, try to break few and check what happens

1. What happens if `Task.WhenAny` returns the first completed task and the next task throws an exception?

1. Explore the benefits of `ConfigureAwait(false)`