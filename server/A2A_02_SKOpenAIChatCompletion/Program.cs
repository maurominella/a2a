// Copyright (c) Microsoft. All rights reserved.

#region Libraries and Namespaces
using A2A; // contains the AISettings class
using A2A.AspNetCore; // contains the SKAgent class
using SKAgentNamespace; // contains the SKCompletionAgent class, e.g. the agent
using AgentTools; // Namespace for the agent AgentCardPrinter
#endregion

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Information));

// ASP.NET Core minimal API to expose the agent via HTTP endpoint
var app = builder.Build();

// Create and register your agent
var taskManager = new TaskManager();
var agent = new SKCompletionAgent();

var agent_url = "http://localhost:5002";
var agent_urlpath = "/LightsAgent";

AgentCard agentCard = await agent.GetAgentCardAsync(agent_url + agent_urlpath, CancellationToken.None);

agent.Attach(taskManager);

app.MapGet("/", () => AgentCardPrinter.RenderAgentIdentityCard(agentCard));
app.MapA2A(taskManager, agent_urlpath);
app.MapWellKnownAgentCard(taskManager, agent_urlpath);
app.MapHttpA2A(taskManager, agent_urlpath);

app.Run();