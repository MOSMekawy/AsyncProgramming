// Example on using Task.Run 

// Task.Run is used to run a method asynchronously on a thread pool thread.
// It returns a Task<TResult> object that represents the asynchronous operation.
var result = await Task.Run(() => 1024);

Console.WriteLine($"Expected number is 1024, Actual number is {result}.");

// Example on using Continuations 
// Inside a method by using the async await keyword a continuation is introduced 
// Another way to incorporate a continuation into your code is using the ContinueWith method
var task = Task.Run(() => 1024);

await task.ContinueWith(t =>
{
    Console.WriteLine($"Continuation #1, Received number {t.Result}");
}).ContinueWith(t =>
{
    // ContinueWith returns a Task object, so you can chain multiple ContinueWith calls
    Console.WriteLine("Continuation #2.");
});

// Handling exceptions

var exceptionTask = Task.Run(() => throw new InvalidOperationException());

await exceptionTask.ContinueWith(t =>
{
    Console.WriteLine($"Exception: {t.Exception.InnerException.Message}");
}).ContinueWith(t =>
{
    Console.WriteLine(t.Exception?.Message ?? "No Exception after first continuation.");
});


// TaskContinuationOptions are used to specify how a continuation should be scheduled
// TaskContinuationOptions.OnlyOnRanToCompletion - continuation is scheduled only if the antecedent task completed successfully
// TaskContinuationOptions.OnlyOnFaulted - continuation is scheduled only if the antecedent task threw an exception
// TaskContinuationOptions.OnlyOnCanceled - continuation is scheduled only if the antecedent task was canceled

// Both examples throw Task Canceled Exception
try
{
    // Completed Task Faulted would not be printed
    await Task.Run(() => Console.WriteLine("Task Completed")).ContinueWith(t => Console.WriteLine("Completed Task Faulted."), TaskContinuationOptions.OnlyOnFaulted);

} catch(Exception ex)
{

}

try
{
    // Faulted Task Completed would not be printed
    await Task.Run(() => throw new InvalidOperationException()).ContinueWith(t => Console.WriteLine("Faulted Task."), TaskContinuationOptions.OnlyOnRanToCompletion);
} catch(Exception ex)
{

}


// Continuations using async await keyword
// Async keyword is used to mark a method as an asynchronous method
// Inside an async method one can use the await keyword to wait for the completion of a task
await Task.Run(() => Console.WriteLine("Heavy computations are being done."));

var cts = new CancellationTokenSource();

// OperationCanceledException is thrown when the cancellation token is canceled
// 
try
{
    Task.Run(() =>
    {
        // Cancellation Token 
        for (var i = 0; i < 10000; i++)
        {
            if (cts.Token.IsCancellationRequested)
            {
                cts.Token.ThrowIfCancellationRequested();
            }
        }
    });
} catch(Exception ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}

cts.Cancel();

// Precomputed Tasks
var calculation = await Task.FromResult(1024);

// Awaiting multiple tasks
var mockTask0 = Task.FromResult(1024);
var mockTask1 = Task.FromResult(2048);
await Task.WhenAll(mockTask0, mockTask1);

// Await till one of the tasks is completed
await Task.WhenAny(mockTask0, mockTask1);

// ConfigureAwait(false) configures the task so that continuation after the await does not have to be run in the caller context, therefore avoiding any possible deadlocks.
await Task.Run(() => Console.WriteLine("Some operations being done !!!")).ConfigureAwait(false);

try
{
    // AggregateException is thrown when multiple exceptions are thrown
    var exceptions = new List<Exception>();

    exceptions.Add(new ArgumentException("Argument Exception Message"));
    exceptions.Add(new NullReferenceException("Null Reference Exception Message"));

    throw new AggregateException("Aggregate Exception Message", exceptions);
} catch(AggregateException ex)
{
    foreach(var innerException in ex.InnerExceptions)
    {
        Console.WriteLine($"Exception: {innerException.Message}");
    }
}

// Progress Reporting using IProgress<T> interface, IProgress<T> interface is used to report progress of an asynchronous operation.

IProgress<int> progress = new Progress<int>(percent =>
{
    Console.WriteLine($"Progress: {percent}%");
});

await Task.Run(() =>
{
    for (var i = 0; i <= 100; i++)
    {
        progress.Report(i);
        Thread.Sleep(100);
    }
});

// Example on using TaskCompletionSource<T> class to communicate between threads,
// Could be used to return an awaitable task from a method where its inner workings is based on callbacks instead of tasks and continuations.

var taskCompletionFunc = () =>
{
    var tcs = new TaskCompletionSource<int>();

    // To mimic a callback based method
    Task.Run(() =>
    {
        Thread.Sleep(1000);
        tcs.SetResult(1024);
    });

    return tcs.Task;
};

Console.WriteLine($"Task completion source example --> Result value is {await taskCompletionFunc()}");

// Example on attached and attached child tasks
// Task.Run() by default creates a detached task
// Task.Run() by default unwraps the task, so that the result of the task is returned instead of the task itself
// Task.Run() is equivalent to Task.Factory.StartNew().Unwrap()  

var parentTask = Task.Run(() =>
{
    Console.WriteLine("Parent task started.");

    var childTask = Task.Factory.StartNew(() => 
    {
        Console.WriteLine("Child task started.");
        Thread.Sleep(1000);
        Console.WriteLine("Child task completed.");
    }, TaskCreationOptions.AttachedToParent);

    Console.WriteLine("Parent task completed.");
});

// example on parallel programming
// Parallel.Invoke() method is used to execute multiple actions in parallel
// Parallel.For() same as ForEach() is blocking to parent thread
Parallel.For(0, 10, i =>
{
    Console.WriteLine($"Parallel.For loop, i = {i}");
});

// An exception in one of the actions will cause the Parallel.Invoke() method to throw an AggregateException
// Execution of the Parallel.Invoke() method is not halted when an exception is thrown in one of the actions
try
{
    Parallel.Invoke(
        () => throw new NullReferenceException("Null value not permitted"),
        () => Console.WriteLine("Parallel.Invoke() method is not halted when an exception is thrown in one of the actions")
    );
} catch(Exception ex)
{
    Console.WriteLine(ex.Message);
}

// Use lock statement to synchronize access to a shared resource
// Use Interlocked class to perform atomic operations on a shared resource

// An example on deadlocks 
/*
    object lock1 = new();
    object lock2 = new();

    var deadlocked0 = Task.Run(() =>
    {
        lock (lock1)
        {
            Thread.Sleep(100);
            lock (lock2)
            {
                Console.WriteLine("Thread 1");
            }
        }
    });

    var deadlocked1 = Task.Run(() =>
    {
        lock (lock2)
        {
            Thread.Sleep(100);
            lock (lock1)
            {
                Console.WriteLine("Thread 2");
            }
        }
    });

    await Task.WhenAll(deadlocked0, deadlocked1);

    // This message won't be printed
    Console.WriteLine("Deadlock example completed.");
*/

// One could cancel parrallel operations using cancellation tokens
// Using cancellation tokens with parallel operations results in an exception.
new ParallelOptions
{
    // Defined period of time after which the cancellation token is canceled.
    // This acts as a timeout.
    CancellationToken = new CancellationTokenSource(2000).Token
};

// Use ThreadLocal class to create thread local variables that has unique value per execution context
var threadLocal = new ThreadLocal<int>(() => 0);

Parallel.For(0, 50, new ParallelOptions { MaxDegreeOfParallelism = 1 }, (i) =>
{
    var threadLocalValue = threadLocal.Value;
    threadLocal.Value = i;
});

// Use AsyncLocal class to create async local variables that are unique per async flow
var asyncLocal = new AsyncLocal<int>(); 
asyncLocal.Value = 0;

Parallel.For(0, 50, new ParallelOptions { MaxDegreeOfParallelism = 1 }, (i) =>
{
    var asyncLocalValue = asyncLocal.Value;
    asyncLocal.Value = i;
});

// Use of Parallel LINQ (PLINQ) to perform parallel operations on collections
// PLINQ could convert a sequential query to a parallel query
// PLINQ queries are analyzed and depending on analysis results, the query is executed in parallel or sequentially
// One could force PLINQ to execute a query in parallel by using the WithExecutionMode() method

var source = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

var query = source.AsParallel()
    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
    .Where(i => i % 2 == 0)
    .AsSequential()
    .Select(i => i * 200);

// One could return once more to sequential execution by using the AsSequential() method

Console.ReadLine();

class MockService
{
    // Example on asynchronous streams
    public static async IAsyncEnumerable<int> GetNumberStreamAsync()
    {
        var rnd = new Random();
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(rnd.Next(1000));
            yield return await Task.FromResult(rnd.Next(200));
        }
    }
}