using A2A;
using System.Net.ServerSentEvents;

Console.Write("Enter the port number where the agent is running (e.g., 5001): http://localhost:");
var port = Console.ReadLine();
// Discover agent and create client
var cardResolver = new A2ACardResolver(new Uri($"http://localhost:{port}/"));
AgentCard agentCard = cardResolver.GetAgentCardAsync().Result;

A2AClient client = new A2AClient(new Uri(agentCard.Url));

Console.Write($@"
    Please ask me something, or type 'EXIT' to end the conversation.
    Examples of questions you can ask:
    - Qual è la ricetta della pizza? (e.g. Basic Chat Completion),
    - Toggle the porch light and tell me all the states (e.g. normal Chat Completion),
    - Toggle the chandelier and tell me the status of all lights (e.g. Plugin usage),
    - What's the warranty coverage for the TrailMaster X4 Tent? (e.g. Copilot Studio),
    - Modifica la luce del portico e dammi lo stato di tutte le luci

    Your turn > ");

// Collect user input
string? question = Console.ReadLine();

var Message = new A2A.AgentMessage()
{
    Role = MessageRole.User,
    Parts = [new TextPart { Text = question }]
};

// Send the message using non-streaming API
Console.WriteLine("\n=== Non-Streaming Communication ===");
A2A.A2AResponse response = await client.SendMessageAsync(new MessageSendParams{Message = Message});
Console.WriteLine($"Received: {((A2A.TextPart)(((A2A.AgentMessage)response).Parts[0])).Text}");
/*
// Send the message using streaming API
Console.WriteLine("\n=== Streaming Communication ===");
await foreach (SseItem<A2AEvent> sseItem in client.SendMessageStreamingAsync(new MessageSendParams { Message = Message }))
{
    var streamingResponse = sseItem.Data;
    Console.WriteLine($"Received streaming chunk: {((A2A.TextPart)(((A2A.AgentMessage)streamingResponse).Parts[0])).Text}");
}
*/