using Grpc.Core;

namespace GrpcDemo.Server;

// Demo.DemoBase is generated from demo.proto at build time.
public class DemoService : Demo.DemoBase
{
    private readonly ILogger<DemoService> _logger;

    public DemoService(ILogger<DemoService> logger) => _logger = logger;

    // 1. UNARY ---------------------------------------------------------------
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        // Metadata = gRPC's version of HTTP headers (auth tokens, correlation IDs, ...).
        var requestId = context.RequestHeaders.GetValue("x-request-id") ?? "none";
        _logger.LogInformation("SayHello name='{Name}' request-id={RequestId}", request.Name, requestId);

        // Errors are signalled with a status code, not an HTTP status.
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is required"));

        return Task.FromResult(new HelloReply { Message = $"Hello, {request.Name}!" });
    }

    // 2. SERVER STREAMING ----------------------------------------------------
    public override async Task CountDown(
        CountRequest request,
        IServerStreamWriter<CountReply> responseStream,
        ServerCallContext context)
    {
        for (var i = request.From; i >= 0; i--)
        {
            // If the client cancels or its deadline passes, stop doing work.
            context.CancellationToken.ThrowIfCancellationRequested();

            await responseStream.WriteAsync(new CountReply { Value = i });
            await Task.Delay(500, context.CancellationToken);
        }
    }

    // 3. CLIENT STREAMING ----------------------------------------------------
    public override async Task<SumReply> Sum(
        IAsyncStreamReader<NumberRequest> requestStream,
        ServerCallContext context)
    {
        int total = 0, count = 0;

        // Loop ends when the client calls CompleteAsync().
        await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
        {
            _logger.LogInformation("Sum received {Value}", message.Value);
            total += message.Value;
            count++;
        }

        return new SumReply { Total = total, Count = count };
    }

    // 4. BIDIRECTIONAL STREAMING ---------------------------------------------
    public override async Task Chat(
        IAsyncStreamReader<ChatMessage> requestStream,
        IServerStreamWriter<ChatMessage> responseStream,
        ServerCallContext context)
    {
        await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
        {
            _logger.LogInformation("Chat from {User}: {Text}", message.User, message.Text);

            await responseStream.WriteAsync(new ChatMessage
            {
                User = "Server",
                Text = $"Echo: {message.Text}"
            });
        }
    }

    // 5. DEADLINES -----------------------------------------------------------
    public override async Task<HelloReply> Slow(HelloRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Slow call started (takes 5s unless cancelled)");

        // Passing the cancellation token means the work stops when the client's deadline expires.
        await Task.Delay(TimeSpan.FromSeconds(5), context.CancellationToken);

        return new HelloReply { Message = "Finished slow work" };
    }
}
