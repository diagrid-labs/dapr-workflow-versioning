# Diagnostics Workflow

```mermaid
flowchart TD
    Start([Start DiagnosticsWorkflow]) --> Hull[AnalyzeHullActivity]
    Hull --> WarpCore[AnalyzeWarpCoreActivity]
    WarpCore --> Security[AnalyzeSecuritySystemsActivity]
    Security --> Recommend[GenerateRecommendationsActivity]
    Recommend --> Notify[NotifyBridgeActivity]
    Notify --> End([Return DiagnosticsOutput])
```
