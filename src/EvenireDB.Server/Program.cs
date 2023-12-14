using EvenireDB;
using EvenireDB.Server.Routes;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();

var serverConfig = builder.Configuration.GetSection("Server").Get<EvenireServerSettings>()!;

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.ListenAnyIP(serverConfig.HttpSettings.Port, o => o.Protocols = HttpProtocols.Http1AndHttp2);
    options.ListenAnyIP(serverConfig.GrpcSettings.Port, o => o.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();

builder.Services.AddHealthChecks();
builder.Services.AddApiVersioning();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
            ctx.ProblemDetails.Extensions.Add("nodeId", Environment.MachineName));

builder.Services
   .AddEvenire(serverConfig)
   .AddSingleton<EventMapper>();

var app = builder.Build();
app.UseExceptionHandler(exceptionHandlerApp
    => exceptionHandlerApp.Run(async context => await Results.Problem().ExecuteAsync(context)));

app.MapGrpcService<EvenireDB.Server.Grpc.EventsGrcpServiceImpl>();
app.MapGrpcHealthChecksService();

app.MapGet("/", () => Results.Redirect("/healthz"));
app.MapHealthChecks("/healthz");
app.MapEventsRoutes();
   
app.Lifetime.ApplicationStarted.Register(() =>
{
    var version = Assembly.GetExecutingAssembly().GetName().Version;
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n\n********************************************************\n");
    
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"EvenireDB Server v{version} is running on environment '{builder.Environment.EnvironmentName}'!");
    Console.WriteLine("Server configuration:");

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine(JsonSerializer.Serialize(serverConfig, new JsonSerializerOptions()
    {
        WriteIndented = true
    }));

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n********************************************************\n\n");

    Console.ResetColor();
});

app.Run();

public partial class Program
{ }
