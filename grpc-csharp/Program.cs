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
builder.Services.AddLogging(
    x => x.ClearProviders()
        .AddConsole()
);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGet(
    "/",
    async ([FromServices] MasterService.MasterServiceClient client, [FromServices] ILogger<Program> logger) =>
    {
        var cts = new CancellationTokenSource();
        var task1 = Task.Run(
            async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    await client.GetMasterDataAsync(new MasterRequest {Query = "Master"});
                }
            }
        );
        var iterations = 0;
        var task2 = Task.Run(
            async () =>
            {
                while (iterations < 12)
                {
                    logger.LogInformation("Waiting already {time}s", iterations * 5);
                    await Task.Delay(5000);
                    iterations++;
                }

                await cts.CancelAsync();
            }
        );
        logger.LogInformation("Launched grpc calling");
        await Task.WhenAll(task1, task2);
        return Results.Ok("Done");
    }
);

app.MapGet(
    "/http",
    async ([FromServices] ILogger<Program> logger) =>
    {
        var cts = new CancellationTokenSource();
        var task1 = Task.Run(
            async () =>
            {
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(
                    builder.Configuration.GetConnectionString("HttpGoServiceUrl")
                    ?? throw new InvalidOperationException("GrpcGoServiceUrl was not configured")
                );

                while (!cts.IsCancellationRequested)
                {
                    using var message = new HttpRequestMessage(HttpMethod.Get, "/")
                    {
                        Content = JsonContent.Create(new {Message = "Master"})
                    };

                    await httpClient.SendAsync(message);
                }
            }
        );
        var iterations = 0;
        var task2 = Task.Run(
            async () =>
            {
                while (iterations < 12)
                {
                    logger.LogInformation("Waiting already {time}s", iterations * 5);
                    await Task.Delay(5000);
                    iterations++;
                }

                await cts.CancelAsync();
            }
        );
        logger.LogInformation("Launched grpc calling");
        await Task.WhenAll(task1, task2);
        return Results.Ok("Done");
    }
);

app.Run();