using Grpc.Core;
using Grpc.Net.Client;
using GrpcDemo;

// A channel wraps the HTTP/2 connection. Create it once and reuse it.
using var channel = GrpcChannel.ForAddress("http://localhost:5002");
var client = new Demo.DemoClient(channel);

// 1. UNARY ------------------------------------------------------------------
Console.WriteLine("=== 1. Unary (with metadata) ===");
var headers = new Metadata { { "x-request-id", Guid.NewGuid().ToString() } };
var reply = await client.SayHelloAsync(new HelloRequest { Name = "World" }, headers);
Console.WriteLine(reply.Message);

// 2. ERROR HANDLING ---------------------------------------------------------
Console.WriteLine("\n=== 2. Error handling ===");
try
{
    await client.SayHelloAsync(new HelloRequest { Name = "" });
}
catch (RpcException ex)
{
    Console.WriteLine($"Caught {ex.StatusCode}: {ex.Status.Detail}");
}

// 3. SERVER STREAMING -------------------------------------------------------
Console.WriteLine("\n=== 3. Server streaming (countdown) ===");
using (var call = client.CountDown(new CountRequest { From = 5 }))
{
    await foreach (var item in call.ResponseStream.ReadAllAsync())
        Console.WriteLine($"Tick: {item.Value}");
}

// 4. CLIENT STREAMING -------------------------------------------------------
Console.WriteLine("\n=== 4. Client streaming (sum) ===");
using (var sumCall = client.Sum())
{
    foreach (var n in new[] { 10, 20, 30, 40 })
    {
        Console.WriteLine($"Sending {n}");
        await sumCall.RequestStream.WriteAsync(new NumberRequest { Value = n });
    }

    await sumCall.RequestStream.CompleteAsync(); // tell the server we're done
    var result = await sumCall.ResponseAsync;
    Console.WriteLine($"Total = {result.Total} from {result.Count} numbers");
}

// 5. BIDIRECTIONAL STREAMING ------------------------------------------------
Console.WriteLine("\n=== 5. Bidirectional streaming (chat) ===");
using (var chat = client.Chat())
{
    // Read responses in the background while we keep sending.
    var reader = Task.Run(async () =>
    {
        await foreach (var message in chat.ResponseStream.ReadAllAsync())
            Console.WriteLine($"  <- {message.User}: {message.Text}");
    });

    foreach (var text in new[] { "Hi", "How are you?", "Bye" })
    {
        Console.WriteLine($"  -> Client: {text}");
        await chat.RequestStream.WriteAsync(new ChatMessage { User = "Client", Text = text });
        await Task.Delay(300);
    }

    await chat.RequestStream.CompleteAsync();
    await reader;
}

// 6. DEADLINE ---------------------------------------------------------------
Console.WriteLine("\n=== 6. Deadline (server needs 5s, we allow 2s) ===");
try
{
    await client.SlowAsync(
        new HelloRequest { Name = "slow" },
        deadline: DateTime.UtcNow.AddSeconds(2));
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
{
    Console.WriteLine("DeadlineExceeded, as expected.");
}

Console.WriteLine("\nDone.");
