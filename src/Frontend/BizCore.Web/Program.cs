using BizCore.Web.Components;
using MudBlazor.Services;
using Fluxor;
using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Toast;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Fluxor for state management
builder.Services.AddFluxor(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
    options.UseReduxDevTools();
});

// Add Blazored services
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredModal();
builder.Services.AddBlazoredToast();

// Add HTTP client
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:5001/");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BizCore.Web._Imports).Assembly);

app.Run();