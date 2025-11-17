using Chatbot.Components;
using Chatbot.Services.Core;
using Chatbot.Services.Modules.CuringRoom;

var builder = WebApplication.CreateBuilder(args);

// ------------------ BLAZOR + API CONTROLLER ------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ------------------ ??NG KÝ CÁC SERVICE ------------------

// HttpClient dùng qua IHttpClientFactory (an toàn, không dispose s?m)
builder.Services.AddHttpClient("DefaultClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // B? qua l?i SSL cho IP n?i b? (10.x.x.x)
    ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true
});

// Core services
builder.Services.AddScoped<SqlExecutor>();
builder.Services.AddScoped<ApiCaller>();
builder.Services.AddScoped<LlmRouterService>();

// Module-specific services
builder.Services.AddScoped<CuringSqlService>();
builder.Services.AddScoped<CuringApiService>();

// RouterService ph?i là Scoped ?? t?o scope m?i khi detect module
builder.Services.AddScoped<RouterService>();

// ------------------ BUILD APP ------------------
var app = builder.Build();

// ------------------ CONFIGURE PIPELINE ------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
