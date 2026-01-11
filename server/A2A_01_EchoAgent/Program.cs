using A2A;
using A2A.AspNetCore;

using AgentServer; // if EchoAgent file is in namespace "AgentServer"
using AgentTools; // Namespace for the agent AgentCardPrinter

// Create the A2A Task Manager to manage agent lifecycle and message routing
var taskManager = new A2A.TaskManager();

// Create and register your agent
var agent = new EchoAgent();

// Attach the agent to the task manager
agent.Attach(taskManager);

var agent_url = "http://localhost:5001";
var agent_urlpath = "/echo";

AgentCard agentCard = await agent.GetAgentCardAsync2(agent_url + agent_urlpath, CancellationToken.None);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => AgentCardPrinter.RenderAgentIdentityCard(agentCard)); // just for testing
app.MapWellKnownAgentCard(taskManager, agent_urlpath);
app.MapHttpA2A(taskManager, agent_urlpath);
app.MapA2A(taskManager, agent_urlpath);

app.Run();