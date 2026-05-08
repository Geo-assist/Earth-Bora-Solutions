using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Agrovet.AdminApp;
using Agrovet.AdminApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => 
{
    var client = new HttpClient {BaseAddress = new Uri("https://altha-proexpert-elliana.ngrok-free-dev/")};
    client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
    return client;
});

builder.Services.AddSingleton<AdminAuthService>();

await builder.Build().RunAsync();
