# 2.4 - Introducing async and await

## Asynchronous programming

### Overview
- `async` & `await` keywords
- Suitable for I/O operations
- Asynchronous operations occurs in parallel, but it subscribes to when that operation completes
- `Task` represents asynchronous operation
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
  - Re-throws exceptions that occured inside the `Task`, if task failed

  ```cs
  async void Search_Click(...)
  {
      var getStocksTask = GetStocks();
      await getStocksTask;
  
      // Everything after await is a continuation
  
      AfterLoadingStockData();
  }
  
  async Task GetStocks()
  {
      try
      {
          var store = new DataStore();
          var responseTask = store.GetStockPrices(StockIdentifier.Text);
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

    Task TaskMethod()
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
await getStocksTask; // Execute code
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
    var store = new DataStore();
    var responseTask = store.GetStockPrices(StockIdentifier.Text);
    Stocks.ItemsSource = await responseTask; // Exception thrown here
    // Task status is set to Faulted, with no exception attached
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
          var store = new DataStore();
          var responseTask = store.GetStockPrices(StockIdentifier.Text);
          Stocks.ItemsSource = await responseTask; // Exception thrown here
      }
      catch (Exception ex) // Demo try catch, it's useless (lost stack trace)
      {
          // The return type is void, not a Task, exception can't be set on a void,
          // so it's thrown back to the caller and app crashes.
          // Returning an exception in Task would be correct (compiler does it automatically).
          throw ex;

          // throw removal will help
      }
  }
  ```
  
  - When working with `async void` whole code in method should be in `try`, `catch`, `finally` blocks without `throw`,
    so it makes sure no exception is thrown back to the caller (prevents app crash)

# 2.8 - Best practices

- Asynchronous ASP.NET relieves web server of work and it can take care of other requests while asynchronous
  operations are running
- `.Result` or `Wait()` on `Task` variable will block thread, execution will run synchronously
  and it may deadlock whole application
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
    - Execute work on a different thread
    - Get the result from asynchronous operation
    - Subscribe to when operation is done + continuation
    - Exception handling

## Task.Run static method

`Task.Run()` queues methods on the thread pool for execution.

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
    var lines = File.ReadAllLines("file.csv"); // Assuming that async version isn't available
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

- When not awaiting `Task.Run`, the task scheduler will queue this to the thread pool,
  it will execute that whenever there is an available thread.
  This call will complete immediately and code execution will continue (no await)

```cs
Task.Run(() => {});
```

# 3.3 - Obtaining the async result without async await keywords

- Alternative approach to subscribe to an async operation
- `async` & `await` is easier to read, has less code and is less error prone

```cs
void Search_Click(...)
{
    try
    {
        var loadLinesTask = Task.Run(() => File.ReadAllLines("StockPrices_Small.csv"));

        // ContinueWith allows for continuation and it will run when task has finished, but
        // in contrast to await it won't execute delegate on the original thread
        var processStocksTask = loadLinesTask.ContinueWith(completedTask =>
        {
            // Task has completed, so using Result is ok here,
            // it won't lock any thread, it just contains what the task returns
            var lines = completedTask.Result;

            // process data...
        }
    }
    catch (Exception ex)
    {
        Notes.Text = ex.Message;
    }
}
```

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
        // Thread 2
    });
    // Thread 1
}
```

# 3.5 - Handling Task success and failure

- No matter how async operation is executed, it should always be validated (exception handling).
  To validate task execution either use:
  - `async` & `await`
    
  ```cs
  try
  {
      await task;
  }
  catch (Exception e)
  {
      // Log e.Message
  }
  ```
  - Chain `ContinueWith()` with error handling options
    
  ```cs
  await task.ContinueWith(t =>
  {
      // Log t.Exception.Message (aggregate exception)
  }, TaskContinuationOptions.OnlyOnFaulted);
  ```

- Working with `ContinueWith()`:
  
```cs
var loadLinesTask = Task.Run(() => throw new FileNotFoundException());

loadLinesTask.ContinueWith(completedTask => { /* Will always run */ });

loadLinesTask.ContinueWith(completedTask =>
{
    // Will run if successfully completed
}, TaskContinuationOptions.OnlyOnRanToCompletion);
```
# 3.6 - 