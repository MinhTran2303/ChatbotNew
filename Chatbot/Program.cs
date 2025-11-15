using Chatbot.Components;
using Chatbot.Services.Core;

var builder = WebApplication.CreateBuilder(args);


// Thêm h? tr? Blazor UI (Server-side)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Thêm controller ?? dùng API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ------------------ ??NG KÝ SERVICE ------------------

// D?ch v? lõi Chatbot
builder.Services.AddSingleton<RouterService>();   // ??c modules.json
builder.Services.AddScoped<SqlExecutor>();        // Th?c thi SQL
builder.Services.AddScoped<ApiCaller>();          // G?i API n?i b?
builder.Services.AddScoped<LlmRouterService>();   // G?i model Ollama
builder.Services.AddHttpClient();                 // Client dùng chung

// ------------------ XÂY D?NG APP ------------------

var app = builder.Build();

// ------------------ C?U HÌNH PIPELINE ------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    // Khi ch?y Development (m?c ??nh)
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
