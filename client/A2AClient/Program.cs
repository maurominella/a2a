using A2A;
using System.Net.ServerSentEvents;

Console.Write("Enter the port number where the agent is running (e.g., 5001): http://localhost:");
var port = Console.ReadLine();
// Discover agent and create client
var cardResolver = new A2ACardResolver(new Uri($"http://localhost:{port}/"));
AgentCard agentCard = cardResolver.GetAgentCardAsync().Result;

A2AClient client = new A2AClient(new Uri(agentCard.Url));

var question = "Modifica la luce del portico e dammi lo stato di tutte le luci"; // "Toggle the porch light and tell me all the states"; // "Qual è la ricetta della pizza?";  // "Modifica la luce del portico e dammi lo stato di tutte le luci"; // "What is the current weather in Seattle?";
var Message = new A2A.AgentMessage()
{
    Role = MessageRole.User,
    Parts = [new TextPart { Text = question }]
};

// Send the message using non-streaming API
Console.WriteLine("\n=== Non-Streaming Communication ===");
A2A.A2AResponse response = await client.SendMessageAsync(new MessageSendParams{Message = Message});
Console.WriteLine($"Received: {((A2A.TextPart)(((A2A.AgentMessage)response).Parts[0])).Text}");

// Send the message using streaming API
Console.WriteLine("\n=== Streaming Communication ===");
await foreach (SseItem<A2AEvent> sseItem in client.SendMessageStreamingAsync(new MessageSendParams { Message = Message }))
{
    var streamingResponse = sseItem.Data;
    Console.WriteLine($"Received streaming chunk: {((A2A.TextPart)(((A2A.AgentMessage)streamingResponse).Parts[0])).Text}");
}