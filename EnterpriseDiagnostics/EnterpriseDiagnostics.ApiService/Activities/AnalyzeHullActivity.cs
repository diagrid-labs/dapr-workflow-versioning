using System.Text.Json;
using Dapr.AI.Conversation;
using Dapr.AI.Conversation.ConversationRoles;
using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class AnalyzeHullActivity(
    ILogger<AnalyzeHullActivity> logger,
    DaprConversationClient conversationClient) : WorkflowActivity<AnalysisInput, AnalysisResult>
{
    public override async Task<AnalysisResult> RunAsync(
        WorkflowActivityContext context,
        AnalysisInput input)
    {
        LogActivity(logger, input.ShipName);

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
                            "{\"systemName\": \"string\", \"status\": \"string\", \"issues\": [\"string\"], \"healthPercentage\": number}")]
                    },
                    new UserMessage
                    {
                        Name = input.EngineerName.Replace(" ", ""),
                        Content = [new MessageContent(
                            $"Perform a hull integrity analysis for the starship {input.ShipName}. " +
                            $"Diagnostics requested on {input.DiagnosticsDate} by {input.EngineerName}. " +
                            "Analyze hull plating, structural integrity field, micro-fractures, " +
                            "and radiation shielding. Return JSON with systemName, status, issues array, " +
                            "and healthPercentage (0-100).")]
                    }
                })
            ],
            options);

        var json = JsonSerializer.Deserialize<JsonElement>(
            response.Outputs.First().Choices.First().Message.Content);

        return new AnalysisResult(
            json.GetProperty("systemName").GetString() ?? "Hull",
            json.GetProperty("status").GetString() ?? "Unknown",
            JsonSerializer.Deserialize<string[]>(json.GetProperty("issues").GetRawText()) ?? [],
            json.GetProperty("healthPercentage").GetInt32());
    }

    [LoggerMessage(LogLevel.Information, "AnalyzeHullActivity: Analyzing hull for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
