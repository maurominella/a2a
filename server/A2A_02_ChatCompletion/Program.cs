// Copyright (c) Microsoft. All rights reserved.

#region Libraries and Namespaces
using A2A; // contains the AISettings class
using A2A.AspNetCore; // contains the SKAgent class
using SKAgentNamespace; // contains the SKCompletionAgent class, e.g. the agent
#endregion

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Information));

// ASP.NET Core minimal API to expose the agent via HTTP endpoint
var app = builder.Build();

// Create and register your agent
var taskManager = new TaskManager();
var agent = new SKCompletionAgent();
agent.Attach(taskManager);

app.MapA2A(taskManager, "/LightsAgent");
app.MapWellKnownAgentCard(taskManager, "/LightsAgent");
app.MapHttpA2A(taskManager, "/LightsAgent");

app.Run();