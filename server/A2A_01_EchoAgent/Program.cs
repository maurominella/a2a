using A2A;
using A2A.AspNetCore;

using AgentServer; // if EchoAgent file is in namespace "AgentServer"

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Create and register your agent
var taskManager = new TaskManager();
var agent = new EchoAgent();

agent.Attach(taskManager);

app.MapA2A(taskManager, "/echo");
app.MapWellKnownAgentCard(taskManager, "/echo");
app.MapHttpA2A(taskManager, "/echo");

app.Run();