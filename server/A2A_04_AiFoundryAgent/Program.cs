using A2A;
using A2A.AspNetCore;

using AgentServer; // if AiFoundry file is in namespace "AgentServer"
using AgentTools; // Namespace for the agent AgentCardPrinter

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Create and register your agent
var taskManager = new TaskManager();
var agent = await AiFoundryAgent.CreateAsync(); // internally runs new AiFoundryAgent();

var agent_url = "http://localhost:5004";
var agent_urlpath = "/AiFoundry";

AgentCard agentCard = await agent.GetAgentCardAsync(agent_url + agent_urlpath, CancellationToken.None);

agent.Attach(taskManager);

app.MapGet("/", () => AgentCardPrinter.RenderAgentIdentityCard(agentCard));
app.MapA2A(taskManager, agent_urlpath);
app.MapWellKnownAgentCard(taskManager, agent_urlpath);
app.MapHttpA2A(taskManager, agent_urlpath);

app.Run();