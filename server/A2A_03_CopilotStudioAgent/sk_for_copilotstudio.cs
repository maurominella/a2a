#region Libraries and Namespaces
using A2A;
using Microsoft.SemanticKernel.Agents.Copilot;

using LLMSettings;
using Microsoft.Agents.CopilotStudio.Client;
using Microsoft.SemanticKernel; // contains the AISettings class

namespace AgentServer; // Namespace for the agent server
#endregion

public class sk_for_copilotstudio
{
    CopilotStudioAgent? _agent;
    string _agent_name = "Copilot Studio Agent";
    string _agent_description = "Copilot Studio Agent wrapped by Semantic Kernel and A2A";
    // string _agent_instructions = "You are a clever agent";

    public sk_for_copilotstudio()
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
            Name = _agent_name,
            Description = _agent_description,
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

        var response = GenericChatWithAgentAsync(agent: _agent, messageText).Result;

        // create and return an artifact
        var message = new A2A.AgentMessage()
        {
            Role = MessageRole.Agent,
            MessageId = Guid.NewGuid().ToString(),
            ContextId = messageSendParams.Message.ContextId,
            Parts = [new TextPart { Text = response }]
        };

        return Task.FromResult<A2A.A2AResponse>(message);
    }

    private void InitializeAgent()
    {
        #region Environment Configuration
        // Load configuration from environment variables or user secrets.
        var ai_settings = new AISettings();
        string tenantId = ai_settings.GetVariable("ConnectCopilotStudioTenantId");
        string appClientId = ai_settings.GetVariable("ConnectCopilotStudioAppClientId");
        string appClientSecret = ai_settings.GetVariable("ConnectCopilotStudioAgentSecret");
        string EnvironmentId = ai_settings.GetVariable("ConnectCopilotStudioEnvironmentId");
        string SchemaName = ai_settings.GetVariable("ConnectCopilotStudioSchemaName");
        #endregion

        var copilotStudioConnectionSettings = new CopilotStudioConnectionSettings(
            tenantId: tenantId,
            appClientId: appClientId,
            appClientSecret: appClientSecret);

        copilotStudioConnectionSettings.EnvironmentId = EnvironmentId;
        copilotStudioConnectionSettings.SchemaName = SchemaName;

        CopilotClient copilotClient = CopilotStudioAgent.CreateClient(copilotStudioConnectionSettings);
        _agent = new CopilotStudioAgent(copilotClient);        

        Console.WriteLine($"\n\n=========== Agent <{_agent_name}> was initialized ===========\n\n");
    }

    private async Task<string> GenericChatWithAgentAsync(object? agent, string? question = null)
    {
        string? agent_response = "";

        if (agent is not CopilotStudioAgent copilotStudioAgent)
        {
            throw new ArgumentException("Invalid agent type. Expected CopilotStudioAgent.", nameof(agent));
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question cannot be null or empty.", nameof(question));
        }

        Console.WriteLine("\n");

        await foreach (ChatMessageContent chatMessage in copilotStudioAgent.InvokeAsync(question))
        {
            Console.Write(chatMessage.Content);
            agent_response += chatMessage.Content;
        }

        Console.WriteLine("\n\n+++++++++++++++++\n");

        return agent_response;
    }
}