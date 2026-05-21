using Chatbot.Application;
using Chatbot.Application.Workflow;
using Chatbot.Infrastructure.InMemory;
using Chatbot.Platforms.Waha;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<WahaOptions>(options =>
{
    options.ApiKey = Environment.GetEnvironmentVariable("WAHA_API_KEY");
    options.BaseUrl = Environment.GetEnvironmentVariable("WAHA_BASE_URL") is { Length: > 0 } baseUrl
        ? baseUrl
        : "http://localhost:3000";
    options.SendRetryCount = ReadPositiveInt("WAHA_SEND_RETRY_COUNT", 3);
});
builder.Services.Configure<WorkflowOptions>(options =>
{
    options.WorkflowTimeoutMinutes = ReadPositiveInt("WORKFLOW_TIMEOUT_MINUTES", 10);
    options.HumanTookOverTimeoutMinutes = ReadPositiveInt("HUMAN_TOOK_OVER_TIMEOUT_MINUTES", 30);
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IConversationStateStore, InMemoryConversationStateStore>();
builder.Services.AddSingleton<IWorkflowStateStore, InMemoryWorkflowStateStore>();
builder.Services.AddSingleton<IConversationLockProvider, InMemoryConversationLockProvider>();
builder.Services.AddSingleton<IWorkflowDefinitionProvider, MvpWorkflowDefinitionProvider>();
builder.Services.AddSingleton<IWahaAppMessageStore, InMemoryWahaAppMessageStore>();
builder.Services.AddScoped<WorkflowEngine>();
builder.Services.AddScoped<WahaWebhookAdapter>();
builder.Services.AddScoped<WahaWebhookMessageProcessor>();
builder.Services.AddScoped<IOutboundMessageSender, WahaOutboundMessageSender>();
builder.Services.AddHttpClient<WahaSendTextClient>((serviceProvider, httpClient) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<WahaOptions>>().Value;
        httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    })
    .AddStandardResilienceHandler(resilienceOptions =>
    {
        resilienceOptions.Retry.MaxRetryAttempts = ReadPositiveInt("WAHA_SEND_RETRY_COUNT", 3);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.MapWahaWebhookEndpoints();

app.Run();

static int ReadPositiveInt(string name, int defaultValue) =>
    int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value > 0
        ? value
        : defaultValue;

public partial class Program;
