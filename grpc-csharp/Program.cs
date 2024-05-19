using Grpc.Net.Client;
using Masters;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpcClient<MasterService.MasterServiceClient>(
    x =>
    {
        x.Address = new Uri(
            builder.Configuration.GetConnectionString("GrpcGoServiceUrl")
            ?? throw new InvalidOperationException("GrpcGoServiceUrl was not configured")
        );
    }
);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGet(
    "/",
    async ([FromServices] MasterService.MasterServiceClient client) =>
    {
        var reply = await client.GetMasterDataAsync(new MasterRequest {Query = "World"});
        Console.WriteLine("Response: " + reply.Data);
    }
);

app.Run();