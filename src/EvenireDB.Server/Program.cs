var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddApiVersioning();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapGet("/", () => "EvenireDB Server is running!");

var eventsApi = app.NewVersionedApi();
var v1 = eventsApi.MapGroup( "/api/v{version:apiVersion}/events" )
                  .HasApiVersion( 1.0 );
v1.MapGet( "/", () => new[] { new EventDTO("asdasd", null, 1) } );

app.Run();


