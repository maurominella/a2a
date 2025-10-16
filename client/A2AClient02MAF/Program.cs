﻿// using System.Net.ServerSentEvents;
using A2A;
// using AgentTools; // if AgentCardPrinter file is in namespace "AgentTools"

Console.Write("Enter the port number where the agent is running (e.g., 5001): http://localhost:");
var port = Console.ReadLine();
// Discover agent and create client
var agentCardResolver = new A2ACardResolver(new Uri($"http://localhost:{port}/"), agentCardPath: "/.well-known/agent-card.json");
// AgentCard agentCard = agentCardResolver.GetAgentCardAsync().Result;
var a2aAgent = agentCardResolver.GetAIAgentAsync().Result;

// // Just call:
// Console.WriteLine(AgentCardPrinter.RenderAgentIdentityCard(agentCard));

// A2AClient client = new A2AClient(new Uri(agentCard.Url));

Console.Write($@"
    Please ask me something, or type 'EXIT' to end the conversation.
    Examples of questions you can ask:
    - This is a nonsense message for my parrot (e.g. Echo agent)
    - Qual è la ricetta della pizza? (e.g. Basic Chat Completion),
    - Toggle the porch light and tell me all the states (e.g. Semantic Kernel with Plugin),
    - What's the warranty coverage for the TrailMaster X4 Tent? (e.g. Copilot Studio),
    - Che tempo farà domani a Sydney? (e.g. AI Foundry with Bing Grounding)

    Your turn > ");

// Collect user input
string? question = Console.ReadLine();

// var Message = new A2A.AgentMessage()
// {
//     Role = MessageRole.User,
//     Parts = [new TextPart { Text = question }]
// };

// Send the message using non-streaming API
Console.WriteLine("\n=== Non-Streaming Communication ===");
// A2A.A2AResponse response = await client.SendMessageAsync(new MessageSendParams{Message = Message});
var response = await a2aAgent.RunAsync(question);
Console.Write(response.Text);

Console.WriteLine("\n=== Streaming Communication ===");
// A2A.A2AResponse response = await client.SendMessageAsync(new MessageSendParams{Message = Message});
var deltas = a2aAgent.RunStreamingAsync(question);
await foreach(var delta in deltas)
{
    Console.Write(delta.Text);
}

// Console.WriteLine($"+++++++++++ ANSWER +++++++++++\n{((A2A.TextPart)(((A2A.AgentMessage)response).Parts[0])).Text}");
/*
// Send the message using streaming API
Console.WriteLine("\n=== Streaming Communication ===");
await foreach (SseItem<A2AEvent> sseItem in client.SendMessageStreamingAsync(new MessageSendParams { Message = Message }))
{
    var streamingResponse = sseItem.Data;
    Console.WriteLine($"Received streaming chunk: {((A2A.TextPart)(((A2A.AgentMessage)streamingResponse).Parts[0])).Text}");
}
*/