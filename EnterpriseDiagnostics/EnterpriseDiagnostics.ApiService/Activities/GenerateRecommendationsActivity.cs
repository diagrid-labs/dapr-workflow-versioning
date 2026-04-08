using System.Text.Json;
using Dapr.AI.Conversation;
using Dapr.AI.Conversation.ConversationRoles;
using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class GenerateRecommendationsActivity(
    ILogger<GenerateRecommendationsActivity> logger,
    DaprConversationClient conversationClient) : WorkflowActivity<RecommendationsInput, RecommendationsResult>
{
    public override async Task<RecommendationsResult> RunAsync(
        WorkflowActivityContext context,
        RecommendationsInput input)
    {
        LogActivity(logger, input.ShipName);

        var analysisData = JsonSerializer.Serialize(new
        {
            hull = input.HullAnalysis,
            warpCore = input.WarpCoreAnalysis,
            securityProtocols = input.SecurityProtocolsAnalysis,
            weaponSystems = input.WeaponSystemsAnalysis
        });

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
                            "{\"recommendations\": [\"string\"], \"priorities\": [\"string\"]}")]
                    },
                    new UserMessage
                    {
                        Name = input.EngineerName.Replace(" ", ""),
                        Content = [new MessageContent(
                            $"Based on the following diagnostics data for the starship {input.ShipName}, " +
                            $"requested on {input.DiagnosticsDate} by {input.EngineerName}, " +
                            "generate a list of actionable recommendations and a prioritized list of repairs. " +
                            $"Diagnostics data: {analysisData}. " +
                            "Return JSON with recommendations array and priorities array.")]
                    }
                })
            ],
            options);

        var json = JsonSerializer.Deserialize<JsonElement>(
            response.Outputs.First().Choices.First().Message.Content);

        return new RecommendationsResult(
            JsonSerializer.Deserialize<string[]>(json.GetProperty("recommendations").GetRawText()) ?? [],
            JsonSerializer.Deserialize<string[]>(json.GetProperty("priorities").GetRawText()) ?? []);
    }

    [LoggerMessage(LogLevel.Information, "GenerateRecommendationsActivity: Generating recommendations for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
