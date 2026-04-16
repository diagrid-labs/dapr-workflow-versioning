using Microsoft.Extensions.Logging;
using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Activities;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Workflows;

internal sealed partial class DiagnosticsWorkflowV2 : Workflow<DiagnosticsInput, DiagnosticsOutput>
{
    public override async Task<DiagnosticsOutput> RunAsync(
        WorkflowContext context,
        DiagnosticsInput input)
    {
        var logger = context.CreateReplaySafeLogger<DiagnosticsWorkflowV2>();
        LogStart(logger, context.InstanceId, input.ShipName);

        // Define activity tasks to run the four analyses in parallel
        var hullTask = context.CallActivityAsync<AnalysisResult>(
            nameof(AnalyzeHullActivity),
            new AnalysisInput(input.ShipName, input.DiagnosticsDate, input.EngineerName, "Hull"));

        var warpCoreTask = context.CallActivityAsync<AnalysisResult>(
            nameof(AnalyzeWarpCoreActivity),
            new AnalysisInput(input.ShipName, input.DiagnosticsDate, input.EngineerName, "Warp Core"));

        var securityTask = context.CallActivityAsync<AnalysisResult>(
            nameof(AnalyzeSecuritySystemsActivity),
            new AnalysisInput(input.ShipName, input.DiagnosticsDate, input.EngineerName, "Security Protocols"));

        var weaponsTask = context.CallActivityAsync<AnalysisResult>(
            nameof(AnalyzeWeaponSystemsActivity),
            new AnalysisInput(input.ShipName, input.DiagnosticsDate, input.EngineerName, "Weapon Systems"));

        // Fan-out/fan-in: wait for all analyses to complete
        await Task.WhenAll(hullTask, warpCoreTask, securityTask, weaponsTask);

        var hullResult = hullTask.Result;
        var warpCoreResult = warpCoreTask.Result;
        var securityResult = securityTask.Result;
        var weaponsResult = weaponsTask.Result;

        LogAnalysesComplete(logger, context.InstanceId);

        // Generate recommendations based on combined analysis data
        var recommendationsInput = new RecommendationsInput(
            input.ShipName,
            input.DiagnosticsDate,
            input.EngineerName,
            hullResult,
            warpCoreResult,
            securityResult,
            weaponsResult);

        var recommendations = await context.CallActivityAsync<RecommendationsResult>(
            nameof(GenerateRecommendationsActivity),
            recommendationsInput);

        LogRecommendationsComplete(logger, context.InstanceId);

        // Notify the bridge with results
        var notificationInput = new BridgeNotificationInput(
            input.ShipName,
            input.EngineerName,
            recommendations);

        var notification = await context.CallActivityAsync<BridgeNotificationResult>(
            nameof(NotifyBridgeActivity),
            notificationInput);

        LogWorkflowComplete(logger, context.InstanceId);

        return new DiagnosticsOutput(
            input.ShipName,
            input.DiagnosticsDate,
            input.EngineerName,
            recommendations,
            notification.Message);
    }

    [LoggerMessage(LogLevel.Information, "Starting diagnostics workflow {Id} for ship: {ShipName}")]
    static partial void LogStart(ILogger logger, string Id, string ShipName);

    [LoggerMessage(LogLevel.Information, "All analyses complete for workflow {Id}")]
    static partial void LogAnalysesComplete(ILogger logger, string Id);

    [LoggerMessage(LogLevel.Information, "Recommendations generated for workflow {Id}")]
    static partial void LogRecommendationsComplete(ILogger logger, string Id);

    [LoggerMessage(LogLevel.Information, "Diagnostics workflow {Id} complete")]
    static partial void LogWorkflowComplete(ILogger logger, string Id);
}
