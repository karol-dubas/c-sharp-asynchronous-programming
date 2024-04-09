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