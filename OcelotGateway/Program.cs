using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot();
// Set listening port
builder.WebHost.UseUrls("http://*:8000");

var app = builder.Build();
await app.UseOcelot();


app.MapGet("/", () => "Hello World!");

app.Run();
