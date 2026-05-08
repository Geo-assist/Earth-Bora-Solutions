using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Agrovet.WebApp;
using Agrovet.WebApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => 
{
    var client = new HttpClient {BaseAddress = new Uri("https://altha-proexpert-elliana.ngrok-free.dev/")};
    client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
    return client;
});

builder.Services.AddSingleton<AuthStateService>();

await builder.Build().RunAsync();
