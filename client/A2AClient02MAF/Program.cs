using A2A;

var agentCardResolver = new A2ACardResolver(new Uri($"http://localhost:5001/")); // Discover agent card from the A2A service

var a2aAgent = agentCardResolver.GetAIAgentAsync().Result; // Create an A2A agent from the discovered card

string? question = Console.ReadLine(); // Collect user input

var response = await a2aAgent.RunAsync(question); // Send the input to the agent and get a response

Console.WriteLine(response.Text); // Print the response