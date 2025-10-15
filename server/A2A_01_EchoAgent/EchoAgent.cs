using A2A;

namespace AgentServer; // Namespace for the agent server

public class EchoAgent
{
    public EchoAgent()
    {
        InitializeAgent();
    }
    public void Attach(ITaskManager taskManager)
    {
        taskManager.OnMessageReceived = ProcessMessageAsync;
        taskManager.OnAgentCardQuery = GetAgentCardAsync;
    }

    public Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken cancellationToken)
    {

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<AgentCard>(cancellationToken);
        }

        var capabilities = new AgentCapabilities()
        {
            Streaming = true,
            PushNotifications = false,
        };

        return Task.FromResult(new AgentCard()
        {
            Name = "Echo Agent",
            Description = "Echoes messages back to the user",
            Url = agentUrl,
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = []
        });
    }

    private Task<A2A.A2AResponse> ProcessMessageAsync(MessageSendParams messageSendParams, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<A2AResponse>(cancellationToken);
        }

        // process the message
        var messageText = messageSendParams.Message.Parts.OfType<TextPart>().First().Text;

        var response = GenericChatWithAgentAsync(agent: null, messageText).Result;

        // create and return an artifact
        var ResponseMessage = new A2A.AgentMessage()
        {
            Role = MessageRole.Agent,
            MessageId = Guid.NewGuid().ToString(),
            ContextId = messageSendParams.Message.ContextId,
            Parts = [new TextPart { Text = response }]
        };

        return Task.FromResult<A2A.A2AResponse>(ResponseMessage);
    }

    
    private async Task<string> GenericChatWithAgentAsync(object? agent, string? question = null)
    {
        string? agent_response = "";

        Console.WriteLine("\n");

        agent_response = question;
        Console.Write(agent_response);

        Console.WriteLine("\n\n+++++++++++++++++\n");

        return agent_response;
    }

    private void InitializeAgent()
    {        
        Console.WriteLine($"\n\n=========== Agent <EchoAgent> was initialized ===========\n\n");
    }
}