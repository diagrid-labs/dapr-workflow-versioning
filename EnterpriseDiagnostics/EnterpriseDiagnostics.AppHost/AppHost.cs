using System.Reflection;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Diagrid.Aspire.Hosting.Dashboard;
using CopperDusk.Aspire.Hosting.Yaml;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

string executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    ?? throw new("Where am I?");

var statePassword = builder.AddParameter("cache-password", "state-store-123", secret: true);
var state = builder
    .AddValkey("cache", 16379, statePassword)
    .WithContainerName("workflow-state")
    .WithDataVolume("workflow-state-data");

var workflowApp = builder
    .AddProject<Projects.EnterpriseDiagnostics_ApiService>("wf-app")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        LogLevel = "debug",
        ResourcesPaths =
        [
            Path.Join(executingPath, "Resources"),
        ],
    });

workflowApp.WaitFor(state);

var stateComponent = builder.AddYamlFile("dashboard-state", new
{
    apiVersion = "dapr.io/v1alpha1",
    kind = "Component",
    metadata = new { name = "workflow-state-dashboard" },
    spec = new
    {
        type = "state.redis",
        version = "v1",
        metadata = new object[]
        {
            new { name = "redisHost", value = "host.docker.internal:16379" },
            new { name = "redisPassword", value = statePassword },
            new { name = "actorStateStore", value = "true" },
        },
    },
});

builder.AddDiagridDashboard(stateComponent)
    .WaitFor(state);

builder.Build().Run();
