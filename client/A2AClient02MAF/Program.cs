using A2A;

Console.Write("Enter the port number where the agent is running (e.g., 5001): http://localhost:");
var port = Console.ReadLine();

// Discover agent and create client
var agentCardResolver = new A2A.A2ACardResolver(new Uri($"http://localhost:{port}/"), agentCardPath: "/echo/v1/card");

var agentCard = agentCardResolver.GetAgentCardAsync().Result;
Console.WriteLine($"Discovered agent card: {agentCard.ToString()}");

// Get the AI agent from the resolver
var a2aAgent = agentCardResolver.GetAIAgentAsync().Result;

Console.Write("\nPlease ask me something: ");
string? question = Console.ReadLine();

// Send the input to the agent and get a response
var response = await a2aAgent.RunAsync(question);

Console.WriteLine($"\nAgent response: {response.Text}");