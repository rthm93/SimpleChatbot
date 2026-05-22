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
builder.Services.Configure<GeminiOptions>(options =>
{
    options.ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
        ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
    options.Model = Environment.GetEnvironmentVariable("GEMINI_MODEL") is { Length: > 0 } model
        ? model
        : "gemini-2.0-flash";
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IConversationStateStore, InMemoryConversationStateStore>();
builder.Services.AddSingleton<IWorkflowStateStore, InMemoryWorkflowStateStore>();
builder.Services.AddSingleton<IConversationLockProvider, InMemoryConversationLockProvider>();
builder.Services.AddSingleton<IFeatureUnavailableMessageGenerator, GeminiFeatureUnavailableMessageGenerator>();
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

app.MapWahaWebhookEndpoints();

app.Run();

static int ReadPositiveInt(string name, int defaultValue) =>
    int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value > 0
        ? value
        : defaultValue;

public partial class Program;
