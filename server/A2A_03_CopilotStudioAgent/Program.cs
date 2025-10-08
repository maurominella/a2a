using A2A;
using A2A.AspNetCore;

using AgentServer; // if EchoAgent file is in namespace "AgentServer"

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Create and register your agent
var taskManager = new TaskManager();
var agent = new sk_for_copilotstudio();

agent.Attach(taskManager);

app.MapA2A(taskManager, "/copilotstudio");
app.MapWellKnownAgentCard(taskManager, "/copilotstudio");
app.MapHttpA2A(taskManager, "/copilotstudio");

app.Run();