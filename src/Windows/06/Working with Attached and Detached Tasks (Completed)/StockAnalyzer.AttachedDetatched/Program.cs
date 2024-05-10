Console.WriteLine("Starting");

await Task.Factory.StartNew(() =>
    {
        Task.Factory.StartNew(() =>
        {
            Thread.Sleep(1000);
            Console.WriteLine("Completed 1");
        }, TaskCreationOptions.AttachedToParent);
        
        Task.Factory.StartNew(() =>
        {
            Thread.Sleep(2000);
            Console.WriteLine("Completed 2");
        });
    }, TaskCreationOptions.DenyChildAttach);

Console.WriteLine("Completed");
Console.ReadKey();