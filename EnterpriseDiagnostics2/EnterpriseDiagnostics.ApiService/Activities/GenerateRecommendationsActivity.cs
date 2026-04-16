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
            Temperature = 0.7,
            PromptCacheRetention = TimeSpan.FromMinutes(15),
            ResponseFormat = GetResponseFormat()
        };

        var response = await conversationClient.ConverseAsync(
            [
                new ConversationInput(new List<IConversationMessage>
                {
                    new SystemMessage
                    {
                        Content = [new MessageContent(
                            "You are a Starfleet engineering diagnostic system for analysis recommendations.")]
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

    private static Google.Protobuf.WellKnownTypes.Struct GetResponseFormat()
    {
        var responseFormat = new Google.Protobuf.WellKnownTypes.Struct();
        responseFormat.Fields.Add("type", Google.Protobuf.WellKnownTypes.Value.ForString("object"));

        var properties = new Google.Protobuf.WellKnownTypes.Struct();

        var stringType = new Google.Protobuf.WellKnownTypes.Struct();
        stringType.Fields.Add("type", Google.Protobuf.WellKnownTypes.Value.ForString("string"));

        var recommendationsType = new Google.Protobuf.WellKnownTypes.Struct();
        recommendationsType.Fields.Add("type", Google.Protobuf.WellKnownTypes.Value.ForString("array"));
        recommendationsType.Fields.Add("items", Google.Protobuf.WellKnownTypes.Value.ForStruct(stringType));

        var prioritiesType = new Google.Protobuf.WellKnownTypes.Struct();
        prioritiesType.Fields.Add("type", Google.Protobuf.WellKnownTypes.Value.ForString("array"));
        prioritiesType.Fields.Add("items", Google.Protobuf.WellKnownTypes.Value.ForStruct(stringType));

        properties.Fields.Add("recommendations", Google.Protobuf.WellKnownTypes.Value.ForStruct(recommendationsType));
        properties.Fields.Add("priorities", Google.Protobuf.WellKnownTypes.Value.ForStruct(prioritiesType));

        responseFormat.Fields.Add("properties", Google.Protobuf.WellKnownTypes.Value.ForStruct(properties));
        responseFormat.Fields.Add("required", Google.Protobuf.WellKnownTypes.Value.ForList(
            Google.Protobuf.WellKnownTypes.Value.ForString("recommendations"),
            Google.Protobuf.WellKnownTypes.Value.ForString("priorities")));

        return responseFormat;
    }

    [LoggerMessage(LogLevel.Information, "GenerateRecommendationsActivity: Generating recommendations for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
