using System.Text.Json;
using Dapr.AI.Conversation;
using Dapr.AI.Conversation.ConversationRoles;
using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class NotifyBridgeActivity(
    ILogger<NotifyBridgeActivity> logger,
    DaprConversationClient conversationClient) : WorkflowActivity<BridgeNotificationInput, BridgeNotificationResult>
{
    public override async Task<BridgeNotificationResult> RunAsync(
        WorkflowActivityContext context,
        BridgeNotificationInput input)
    {
        LogActivity(logger, input.ShipName);

        var recommendationsData = JsonSerializer.Serialize(input.Recommendations);

        var options = new ConversationOptions("conversation")
        {
            Temperature = 0.7
        };

        var response = await conversationClient.ConverseAsync(
            [
                new ConversationInput(new List<IConversationMessage>
                {
                    new SystemMessage
                    {
                        Content = [new MessageContent(
                            "You are a Starfleet engineering diagnostic system. " +
                            "Respond ONLY with valid JSON, no markdown formatting. " +
                            "Use this exact JSON structure: " +
                            "{\"message\": \"string\"}")]
                    },
                    new UserMessage
                    {
                        Name = input.EngineerName.Replace(" ", ""),
                        Content = [new MessageContent(
                            $"Compose a concise bridge notification message for the starship {input.ShipName}. " +
                            $"The diagnostics were requested by {input.EngineerName}. " +
                            $"Based on these recommendations: {recommendationsData}. " +
                            "Write a professional Starfleet bridge notification summarizing the key findings " +
                            "and top priorities. Return JSON with a message field.")]
                    }
                })
            ],
            options);

        var json = JsonSerializer.Deserialize<JsonElement>(
            response.Outputs.First().Choices.First().Message.Content);

        var message = json.GetProperty("message").GetString()
            ?? "Diagnostics complete. Please review the full report.";

        LogNotification(logger, message);

        return new BridgeNotificationResult(message);
    }

    [LoggerMessage(LogLevel.Information, "NotifyBridgeActivity: Sending bridge notification for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);

    [LoggerMessage(LogLevel.Information, "Bridge notification: {Message}")]
    static partial void LogNotification(ILogger logger, string Message);
}
