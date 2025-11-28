using Chatbot.Components;
using Chatbot.Services.Core;
using Chatbot.Services.Modules.CuringRoom;
using Chatbot.Services.Modules.Rack;
using Chatbot.Services.Modules.Station;


var builder = WebApplication.CreateBuilder(args);


// 1. BLAZOR + API CONTROLLER

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// 2. HTTP CLIENT (bắt buộc để call API nội bộ 10.x.x.x)

builder.Services.AddHttpClient("DefaultClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true
});


// 3. CORE SERVICES

builder.Services.AddScoped<SqlExecutor>();
builder.Services.AddScoped<ApiCaller>();
builder.Services.AddScoped<LlmRouterService>();

// LLM + INTENT DETECTOR
builder.Services.AddSingleton<LlmClient>();
builder.Services.AddScoped<IntentDetector>();

// AI ROUTER (điều phối module)
builder.Services.AddScoped<RouterService>();

// 4. MODULE: CURING ROOM

builder.Services.AddScoped<CuringSqlService>();
builder.Services.AddScoped<CuringApiService>();


// 5. MODULE: RACK MONITORING 

builder.Services.AddScoped<RackApiService>();
// MODULE: STATION
builder.Services.AddScoped<StationApiService>();


// 6. BUILD APP
var app = builder.Build();


// 7. MIDDLEWARE PIPELINE

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

