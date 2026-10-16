using GrpcDemo.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// gRPC requires HTTP/2. For local learning we use plain HTTP/2 (no TLS) on port 5000
// so there are no certificates to trust. In production, always use TLS.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000, listen => listen.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection(); // lets tools like grpcurl discover the API

var app = builder.Build();

app.MapGrpcService<DemoService>();
app.MapGrpcReflectionService();

app.Run();
