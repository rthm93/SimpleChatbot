using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Chatbot.Application;
using Chatbot.Application.Workflow;
using Chatbot.Domain;
using Chatbot.Platforms.Waha;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Chatbot.Tests;

public sealed class ChatbotWorkflowTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Adapter_normalizes_waha_message_payload()
    {
        var payload = DeserializeWahaRequest("""
            {
              "event": "message",
              "session": "default",
              "payload": {
                "id": "false_111@c.us_ABC",
                "timestamp": 1667561485,
                "from": "111@c.us",
                "fromMe": false,
                "to": "bot@c.us",
                "body": "Hello",
                "hasMedia": false
              }
            }
            """);

        var ok = new WahaWebhookAdapter().TryNormalize(payload, out var message);

        Assert.True(ok);
        Assert.NotNull(message);
        Assert.Equal(new ConversationKey(Platform.Waha, "111@c.us"), message.Conversation);
        Assert.Equal(MessageDirection.CustomerToBot, message.Direction);
        Assert.Equal("Hello", message.Text);
        Assert.Equal("false_111@c.us_ABC", message.ExternalMessageId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1667561485), message.Timestamp);
    }

    [Fact]
    public void Adapter_normalizes_webjs_lid_message_payload()
    {
        var payload = DeserializeWahaRequest("""
            {
              "id": "evt_01ks58zy3z1wd66mx8rm3ftjsk",
              "timestamp": 1779367409792,
              "event": "message",
              "session": "default",
              "metadata": {},
              "me": {
                "id": "60103342717@c.us",
                "pushName": "Pc Buddies Solutions"
              },
              "payload": {
                "id": "false_217398577729537@lid_AC1F362892453566E3C6CD94265B0D77",
                "timestamp": 1779367409,
                "from": "217398577729537@lid",
                "fromMe": false,
                "source": "app",
                "to": "60103342717@c.us",
                "body": "Gi",
                "hasMedia": false,
                "media": null,
                "ack": 1,
                "ackName": "SERVER",
                "location": null,
                "vCards": [],
                "_data": {
                  "id": {
                    "fromMe": false,
                    "remote": "217398577729537@lid",
                    "id": "AC1F362892453566E3C6CD94265B0D77",
                    "_serialized": "false_217398577729537@lid_AC1F362892453566E3C6CD94265B0D77"
                  },
                  "viewed": false,
                  "body": "Gi",
                  "type": "chat",
                  "from": "217398577729537@lid",
                  "to": "60103342717@c.us",
                  "mentionedJidList": [],
                  "links": []
                }
              },
              "engine": "WEBJS",
              "environment": {
                "version": "2026.4.2",
                "engine": "WEBJS",
                "tier": "CORE"
              }
            }
            """);

        var ok = new WahaWebhookAdapter().TryNormalize(payload, out var message);

        Assert.True(ok);
        Assert.NotNull(message);
        Assert.Equal(new ConversationKey(Platform.Waha, "217398577729537@lid"), message.Conversation);
        Assert.Equal(MessageDirection.CustomerToBot, message.Direction);
        Assert.Equal("Gi", message.Text);
        Assert.Equal("false_217398577729537@lid_AC1F362892453566E3C6CD94265B0D77", message.ExternalMessageId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1779367409), message.Timestamp);
    }

    [Fact]
    public async Task Endpoints_accept_without_auth_and_reject_malformed_payload()
    {
        await using var app = new ChatbotApplication();
        var client = app.CreateClient();

        var accepted = await client.PostAsJsonAsync("/api/waha/message", WahaMessage("hello"));
        var malformed = await client.PostAsJsonAsync("/api/waha/message", new { payload = new { body = "hello" } });
        var malformedAny = await client.PostAsJsonAsync("/api/waha/message-any", new { payload = new { body = "hello" } });

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malformedAny.StatusCode);
    }

    [Fact]
    public async Task Idle_customer_message_starts_mvp_menu()
    {
        await using var app = new ChatbotApplication();
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("hello"));

        Assert.Contains(MvpWorkflowDefinitionProvider.MenuPrompt, app.Outbound.Messages);
        var state = await app.ConversationStates.GetAsync(Customer, CancellationToken.None);
        Assert.Equal(ConversationState.InProgress, state?.State);
    }

    [Fact]
    public async Task In_progress_menu_handles_one_two_and_invalid_replies()
    {
        await using var app = new ChatbotApplication();
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("hello"));
        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("1"));
        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("2"));
        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("bad"));

        Assert.Contains("Make appointment is still backstage learning its lines. Please check back soon.", app.Outbound.Messages);
        Assert.Contains("Enquiry is still backstage learning its lines. Please check back soon.", app.Outbound.Messages);
        Assert.Equal(MvpWorkflowDefinitionProvider.MenuPrompt, app.Outbound.Messages.Last());
    }

    [Fact]
    public async Task Talk_to_human_sets_human_takeover_without_starting_workflow()
    {
        await using var app = new ChatbotApplication();
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/api/waha/message", WahaMessage(" talk to human "));
        var takeoverState = await app.ConversationStates.GetAsync(Customer, CancellationToken.None);

        Assert.Equal(ConversationState.HumanTookOver, takeoverState?.State);
        Assert.Empty(app.Outbound.Messages);
    }

    [Fact]
    public async Task Restart_during_unexpired_human_takeover_is_ignored()
    {
        await using var app = new ChatbotApplication();
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("talk to human"));
        await client.PostAsJsonAsync("/api/waha/message", WahaMessage(" ReStart "));

        var state = await app.ConversationStates.GetAsync(Customer, CancellationToken.None);
        Assert.Equal(ConversationState.HumanTookOver, state?.State);
        Assert.Empty(app.Outbound.Messages);
    }

    [Fact]
    public async Task Restart_after_human_takeover_timeout_starts_workflow()
    {
        await using var app = new ChatbotApplication();
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("talk to human"));
        app.Time.Advance(TimeSpan.FromMinutes(31));
        await client.PostAsJsonAsync("/api/waha/message", WahaMessage(" ReStart "));

        var state = await app.ConversationStates.GetAsync(Customer, CancellationToken.None);
        Assert.Equal(ConversationState.InProgress, state?.State);
        Assert.Equal([MvpWorkflowDefinitionProvider.MenuPrompt], app.Outbound.Messages);
    }

    [Fact]
    public async Task Workflow_timeout_resets_to_latest_first_block_without_terminating()
    {
        await using var app = new ChatbotApplication();
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("hello", timestamp: 100));
        app.Time.Advance(TimeSpan.FromMinutes(11));
        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("1", timestamp: 800));

        Assert.Equal([MvpWorkflowDefinitionProvider.MenuPrompt, MvpWorkflowDefinitionProvider.MenuPrompt], app.Outbound.Messages);
        var state = await app.ConversationStates.GetAsync(Customer, CancellationToken.None);
        Assert.Equal(ConversationState.InProgress, state?.State);
    }

    [Fact]
    public async Task Human_takeover_ignores_messages_until_idle_timeout_expires()
    {
        await using var app = new ChatbotApplication();
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("talk to human"));
        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("hello"));
        app.Time.Advance(TimeSpan.FromMinutes(31));
        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("hello again"));

        Assert.Equal([MvpWorkflowDefinitionProvider.MenuPrompt], app.Outbound.Messages);
        var state = await app.ConversationStates.GetAsync(Customer, CancellationToken.None);
        Assert.Equal(ConversationState.InProgress, state?.State);
    }

    [Fact]
    public async Task Message_any_unknown_from_me_triggers_manual_takeover_but_known_app_sent_does_not()
    {
        await using var app = new ChatbotApplication();
        var client = app.CreateClient();

        await app.AppMessages.RecordSentMessageIdAsync(Customer, "known-id", CancellationToken.None);
        await client.PostAsJsonAsync("/api/waha/message-any", WahaMessage("app reply", fromMe: true, id: "known-id"));
        Assert.Null(await app.ConversationStates.GetAsync(Customer, CancellationToken.None));

        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("hello"));
        await client.PostAsJsonAsync("/api/waha/message-any", WahaMessage("manual", fromMe: true, id: "unknown-id"));

        var state = await app.ConversationStates.GetAsync(Customer, CancellationToken.None);
        var workflow = await app.WorkflowStates.GetAsync(Customer, CancellationToken.None);
        Assert.Equal(ConversationState.HumanTookOver, state?.State);
        Assert.Null(workflow);
    }

    [Fact]
    public async Task Message_any_absent_from_me_id_triggers_manual_takeover_even_when_text_matches_app_reply()
    {
        await using var app = new ChatbotApplication();
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/api/waha/message", WahaMessage("hello"));
        var appReply = app.Outbound.Messages.Single();

        await client.PostAsJsonAsync("/api/waha/message-any", WahaMessage(appReply, fromMe: true, id: null));

        var state = await app.ConversationStates.GetAsync(Customer, CancellationToken.None);
        var workflow = await app.WorkflowStates.GetAsync(Customer, CancellationToken.None);
        Assert.Equal(ConversationState.HumanTookOver, state?.State);
        Assert.Null(workflow);
    }

    [Fact]
    public async Task Message_any_known_from_me_object_id_does_not_trigger_manual_takeover()
    {
        await using var app = new ChatbotApplication();
        var client = app.CreateClient();

        await app.AppMessages.RecordSentMessageIdAsync(Customer, "known-id", CancellationToken.None);
        await client.PostAsJsonAsync(
            "/api/waha/message-any",
            WahaMessage("app reply", fromMe: true, id: new { _serialized = "known-id", id = "inner-id" }));

        Assert.Null(await app.ConversationStates.GetAsync(Customer, CancellationToken.None));
    }

    [Fact]
    public void Invalid_environment_values_use_defaults()
    {
        var previousRetry = Environment.GetEnvironmentVariable("WAHA_SEND_RETRY_COUNT");
        var previousWorkflowTimeout = Environment.GetEnvironmentVariable("WORKFLOW_TIMEOUT_MINUTES");
        var previousHumanTimeout = Environment.GetEnvironmentVariable("HUMAN_TOOK_OVER_TIMEOUT_MINUTES");

        try
        {
            Environment.SetEnvironmentVariable("WAHA_SEND_RETRY_COUNT", "not-a-number");
            Environment.SetEnvironmentVariable("WORKFLOW_TIMEOUT_MINUTES", "0");
            Environment.SetEnvironmentVariable("HUMAN_TOOK_OVER_TIMEOUT_MINUTES", "-1");

            using var app = new ChatbotApplication();

            Assert.Equal(3, app.Services.GetRequiredService<IOptions<WahaOptions>>().Value.SendRetryCount);
            Assert.Equal(10, app.Services.GetRequiredService<IOptions<WorkflowOptions>>().Value.WorkflowTimeoutMinutes);
            Assert.Equal(30, app.Services.GetRequiredService<IOptions<WorkflowOptions>>().Value.HumanTookOverTimeoutMinutes);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WAHA_SEND_RETRY_COUNT", previousRetry);
            Environment.SetEnvironmentVariable("WORKFLOW_TIMEOUT_MINUTES", previousWorkflowTimeout);
            Environment.SetEnvironmentVariable("HUMAN_TOOK_OVER_TIMEOUT_MINUTES", previousHumanTimeout);
        }
    }

    private static readonly ConversationKey Customer = new(Platform.Waha, "111@c.us");

    private static WahaWebhookRequest DeserializeWahaRequest(string json) =>
        JsonSerializer.Deserialize<WahaWebhookRequest>(json, JsonOptions) ?? throw new InvalidOperationException("Expected a WAHA request.");

    private static object WahaMessage(string text, bool fromMe = false, object? id = null, long timestamp = 1667561485) =>
        new
        {
            eventName = "message",
            session = "default",
            payload = new
            {
                id,
                timestamp,
                from = fromMe ? "bot@c.us" : Customer.ContactId,
                fromMe,
                to = fromMe ? Customer.ContactId : "bot@c.us",
                body = text,
                hasMedia = false
            }
        };

    private sealed class ChatbotApplication : WebApplicationFactory<Program>
    {
        public CapturingOutboundSender Outbound { get; } = new();

        public MutableTimeProvider Time { get; } = new(DateTimeOffset.FromUnixTimeSeconds(1667561485));

        public IConversationStateStore ConversationStates => Services.GetRequiredService<IConversationStateStore>();

        public IWorkflowStateStore WorkflowStates => Services.GetRequiredService<IWorkflowStateStore>();

        public IWahaAppMessageStore AppMessages => Services.GetRequiredService<IWahaAppMessageStore>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.RemoveAll<IOutboundMessageSender>();
                services.AddSingleton<TimeProvider>(Time);
                services.AddSingleton<IOutboundMessageSender>(Outbound);
            });
        }
    }

    private sealed class CapturingOutboundSender : IOutboundMessageSender
    {
        private readonly List<string> _messages = [];

        public IReadOnlyList<string> Messages => _messages;

        public Task<string?> SendTextAsync(ConversationKey conversation, string text, CancellationToken cancellationToken)
        {
            _messages.Add(text);
            return Task.FromResult<string?>($"app-{_messages.Count}");
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration)
        {
            _utcNow += duration;
        }
    }
}
