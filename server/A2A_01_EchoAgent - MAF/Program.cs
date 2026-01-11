using A2A;
using Microsoft.AspNetCore.Builder;
using AgentServer;
using AgentTools;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Create and register your agent
var agent = new EchoAgent();

var agent_urlpath = "/echo";

var taskManager = new TaskManager();

agent.Attach(taskManager);

// Using Microsoft Agent Framework (MAF) A2A extensions
Microsoft.AspNetCore.Builder.MicrosoftAgentAIHostingA2AEndpointRouteBuilderExtensions.MapA2A(app, taskManager, agent_urlpath);
/*
you can call GET http://localhost:5001/echo/v1/card to get the agent card
you can call POST http://localhost:5001/echo/v1/message:send to send messages to the agent
{
  "message": {
    "kind": "message",
    "role": "user",
    "messageId": "msg-001",
    "contextId": "conv-123",
    "parts": [
      { "kind": "text", "text": "Hello Echo Agent!" }
    ]
  }
}
*/

app.Run();