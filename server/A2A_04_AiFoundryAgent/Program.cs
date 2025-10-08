using A2A;
using A2A.AspNetCore;

using AgentServer; // if AiFoundry file is in namespace "AgentServer"

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Create and register your agent
var taskManager = new TaskManager();
var agent = new AiFoundryAgent();

agent.Attach(taskManager);

app.MapA2A(taskManager, "/AiFoundry");
app.MapWellKnownAgentCard(taskManager, "/AiFoundry");
app.MapHttpA2A(taskManager, "/AiFoundry");

app.Run();