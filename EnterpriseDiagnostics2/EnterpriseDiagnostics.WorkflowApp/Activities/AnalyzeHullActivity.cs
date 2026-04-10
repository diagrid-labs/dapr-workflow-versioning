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
                            "You are a Starfleet engineering diagnostic system for teh starship hull.")]
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

    private static Google.Protobuf.WellKnownTypes.Struct GetResponseFormat()
    {
        var responseFormat = new Google.Protobuf.WellKnownTypes.Struct();
        responseFormat.Fields.Add("type", Google.Protobuf.WellKnownTypes.Value.ForString("object"));

        var properties = new Google.Protobuf.WellKnownTypes.Struct();

        var stringType = new Google.Protobuf.WellKnownTypes.Struct();
        stringType.Fields.Add("type", Google.Protobuf.WellKnownTypes.Value.ForString("string"));

        var numberType = new Google.Protobuf.WellKnownTypes.Struct();
        numberType.Fields.Add("type", Google.Protobuf.WellKnownTypes.Value.ForString("integer"));

        var issuesType = new Google.Protobuf.WellKnownTypes.Struct();
        issuesType.Fields.Add("type", Google.Protobuf.WellKnownTypes.Value.ForString("array"));
        issuesType.Fields.Add("items", Google.Protobuf.WellKnownTypes.Value.ForStruct(stringType));

        properties.Fields.Add("systemName", Google.Protobuf.WellKnownTypes.Value.ForStruct(stringType));
        properties.Fields.Add("status", Google.Protobuf.WellKnownTypes.Value.ForStruct(stringType));
        properties.Fields.Add("issues", Google.Protobuf.WellKnownTypes.Value.ForStruct(issuesType));
        properties.Fields.Add("healthPercentage", Google.Protobuf.WellKnownTypes.Value.ForStruct(numberType));

        responseFormat.Fields.Add("properties", Google.Protobuf.WellKnownTypes.Value.ForStruct(properties));
        responseFormat.Fields.Add("required", Google.Protobuf.WellKnownTypes.Value.ForList(
            Google.Protobuf.WellKnownTypes.Value.ForString("systemName"),
            Google.Protobuf.WellKnownTypes.Value.ForString("status"),
            Google.Protobuf.WellKnownTypes.Value.ForString("issues"),
            Google.Protobuf.WellKnownTypes.Value.ForString("healthPercentage")));

        return responseFormat;
    }

    [LoggerMessage(LogLevel.Information, "AnalyzeHullActivity: Analyzing hull for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
