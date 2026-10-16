# gRPC Demo (.NET 8)

A small solution for understanding gRPC. One shared contract, one server, one console client that exercises every call type.

```
GrpcDemo/
├── Protos/demo.proto      <- the contract (shared by server and client)
├── Server/                <- ASP.NET Core gRPC service
│   ├── Program.cs         <- hosting and Kestrel (HTTP/2) setup
│   └── DemoService.cs     <- implementation of every RPC
└── Client/                <- console app that calls the server
    └── Program.cs
```

## Run it

Needs the .NET 8 SDK (or newer) installed.

Terminal 1:
```bash
cd GrpcDemo
dotnet run --project Server
```

Terminal 2:
```bash
cd GrpcDemo
dotnet run --project Client
```

The client prints six sections: unary, error handling, server streaming, client streaming, bidirectional streaming, and a deadline. Watch the server terminal at the same time to see the other side of each call.

## What each part teaches

| Concept | Where to look |
|---|---|
| Contract-first design | `Protos/demo.proto`: message fields have numbers (tags), which is what goes on the wire |
| Code generation | The `<Protobuf>` item in each `.csproj` generates `Demo.DemoBase` (server) and `Demo.DemoClient` (client) |
| Four call types | The four `rpc` lines in the proto, implemented in `DemoService.cs` |
| Metadata (headers) | `x-request-id` sent in `Client/Program.cs`, read in `SayHello` |
| Error handling | `RpcException` + `StatusCode.InvalidArgument` |
| Deadlines and cancellation | `Slow` RPC and `context.CancellationToken` |
| HTTP/2 | `Program.cs` forces `HttpProtocols.Http2`. gRPC cannot run over HTTP/1.1 |
| Channel reuse | One `GrpcChannel` in the client, shared by all calls |

## Experiments to try

1. **Change the contract.** Add `string greeting = 2;` to `HelloRequest`, rebuild, and use it. Then rename a field and notice the wire format does not change, because only the tag number matters.
2. **Break compatibility.** Change a field's number and run an old client against a new server to see why tags must never be reused.
3. **Stop the server mid-stream** (Ctrl+C during the countdown) and see the client get `StatusCode.Unavailable`.
4. **Raise the deadline** in section 6 to 6 seconds and the call succeeds.
5. **Use grpcurl** (reflection is enabled):
   ```bash
   grpcurl -plaintext localhost:5000 list
   grpcurl -plaintext localhost:5000 describe demo.Demo
   grpcurl -plaintext -d '{"name":"CLI"}' localhost:5000 demo.Demo/SayHello
   ```

## Next steps (good interview material)

- Add TLS and a JWT `[Authorize]` attribute on the service
- Add a server `Interceptor` for logging and correlation IDs
- Register the client with `AddGrpcClient<Demo.DemoClient>()` for DI and a retry policy
- Add `Grpc.AspNetCore.HealthChecks` for Kubernetes probes
