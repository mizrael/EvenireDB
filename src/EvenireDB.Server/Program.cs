using EvenireDB;
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

var serverConfig = builder.Configuration.GetSection("Server").Get<EvenireServerSettings>()!;

builder.WebHost.ConfigureKestrel((context, options) =>
{
    if (serverConfig.GrpcSettings.Enabled)
        options.ListenAnyIP(serverConfig.GrpcSettings.Port, o => o.Protocols = HttpProtocols.Http2);

    if (serverConfig.HttpSettings.Enabled)
        options.ListenAnyIP(serverConfig.HttpSettings.Port, o => o.Protocols = HttpProtocols.Http1);
});

if (serverConfig.GrpcSettings.Enabled)
    builder.Services.AddGrpc();

builder.Services
   .AddEvenire(serverConfig)
   .AddSingleton<EventMapper>();   

var version = Assembly.GetExecutingAssembly().GetName().Version;

var app = builder.Build();
app.UseExceptionHandler(exceptionHandlerApp
    => exceptionHandlerApp.Run(async context => await Results.Problem().ExecuteAsync(context)));
app.MapHealthChecks("/healthz");

//TODO: build a proper home page
app.MapGet("/", () => $"EvenireDB Server v{version} is running on environment '{builder.Environment.EnvironmentName}'!");

if (!builder.Environment.IsProduction())
    app.MapGet("/config", () => serverConfig);

if (serverConfig.HttpSettings.Enabled)
    app.MapEventsRoutes();

if (serverConfig.GrpcSettings.Enabled)
    app.MapGrpcService<EvenireDB.Server.Grpc.EventsService>();

app.Run();

public partial class Program
{ }
