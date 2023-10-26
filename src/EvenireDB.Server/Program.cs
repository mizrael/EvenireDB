using EvenireDB;
using EvenireDB.Server;
using EvenireDB.Server.Routes;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddApiVersioning();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
            ctx.ProblemDetails.Extensions.Add("nodeId", Environment.MachineName));

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();

var serverConfig = builder.Configuration.GetSection("Server").Get<EvenireServerSettings>();

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.ListenAnyIP(serverConfig.GrpcPort, o => o.Protocols = HttpProtocols.Http2);
    options.ListenAnyIP(serverConfig.HttpPort, o => o.Protocols = HttpProtocols.Http1);
});

builder.Services.AddGrpc();

builder.Services
    .AddEvenire(serverConfig)
    .AddSingleton<EventMapper>();   

var version = Assembly.GetExecutingAssembly().GetName().Version;

var app = builder.Build();
app.UseExceptionHandler(exceptionHandlerApp
    => exceptionHandlerApp.Run(async context => await Results.Problem().ExecuteAsync(context)));
app.MapHealthChecks("/healthz");
app.MapGet("/", () => $"EvenireDB Server v{version} is running!");

app.MapEventsRoutes();
app.MapGrpcService<EvenireDB.Server.Grpc.EventsService>();

app.Run();

public partial class Program
{ }
