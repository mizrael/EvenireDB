using EvenireDB.Client;

var builder = WebApplication.CreateBuilder(args);

// https://github.com/dotnet/aspnetcore/issues/28174
builder.WebHost.UseStaticWebAssets();

var clientConfig = builder.Configuration.GetSection("Evenire").Get<EvenireClientConfig>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddEvenireDB(clientConfig);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();


app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();