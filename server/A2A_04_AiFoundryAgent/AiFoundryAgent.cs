using A2A;
// dotnet add package Azure.AI.Agents.Persistent --> <PackageReference Include="Azure.AI.Agents.Persistent" Version="1.1.0" />
using Azure.AI.Agents.Persistent;

// dotnet add package Microsoft.SemanticKernel.Agents.AzureAI --prerelease --> <PackageReference Include="Microsoft.SemanticKernel.Agents.AzureAI" Version="1.65.0-preview" />
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using LLMSettings;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.SemanticKernel; // contains the AISettings class

namespace AgentServer; // Namespace for the agent server

public class AiFoundryAgent
{
    AzureAIAgent? sk_aifoundry_agent;
    
    public AiFoundryAgent() { }

    public static async Task<AiFoundryAgent> CreateAsync()
    {
        var agent = new AiFoundryAgent();
        await agent.InitializeAgentAsync(aifoundryagent_id: null);
        return agent;
    }

    public void Attach(ITaskManager taskManager)
    {
        taskManager.OnMessageReceived = ProcessMessageAsync;
        taskManager.OnAgentCardQuery = GetAgentCardAsync;
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
        var message = new A2A.AgentMessage()
        {
            Role = A2A.MessageRole.Agent,
            MessageId = Guid.NewGuid().ToString(),
            ContextId = messageSendParams.Message.ContextId,
            Parts = [new TextPart { Text = response }]
        };

        return Task.FromResult<A2A.A2AResponse>(message);
    }

    private Task<AgentCard> GetAgentCardAsync(string agentUrl, CancellationToken cancellationToken)
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

    private async Task<string> GenericChatWithAgentAsync(object? agent, string? question = null)
    {
        string? agent_response = "";

        Console.WriteLine("\n");

        AzureAIAgentThread agentThread = new(sk_aifoundry_agent.Client);
        ChatMessageContent message = new(AuthorRole.User, question);

        // await foreach (ChatMessageContent response in sk_aifoundry_agent.InvokeAsync(message2, agentThread)) // non-streaming version
        await foreach (StreamingChatMessageContent response in sk_aifoundry_agent.InvokeStreamingAsync(message, agentThread)) // streaming version
        {
            Console.Write(response.Content);
            agent_response += response.Content;
        }

        Console.WriteLine("\n\n+++++++++++++++++\n");

        return agent_response;
    }

    private async Task InitializeAgentAsync(string? aifoundryagent_id)
    {
        #region Environment Configuration
        // Load configuration from environment variables or user secrets.
        var ai_settings = new AISettings();
        #endregion

        PersistentAgent sk_ai_agent_definition;

        var aiproject_client = new AIProjectClient(new Uri(ai_settings.GetVariable("AIF_STD_PROJECT_ENDPOINT")), new AzureCliCredential());
        // we could create the project agent without the project client, but we need it for the deletion
        PersistentAgentsClient aiagents_client = aiproject_client.GetPersistentAgentsClient();

        BingGroundingToolDefinition bingGroundingTool = new(
                bingGrounding: new BingGroundingSearchToolParameters(
                    [new BingGroundingSearchConfiguration(connectionId: ai_settings.GetVariable("BING_CONNECTION_ID"))]
                )
            );

        if (string.IsNullOrWhiteSpace(aifoundryagent_id))
        {
            sk_ai_agent_definition = await aiagents_client.Administration.CreateAgentAsync(
                model: ai_settings.AzureOpenAI.ChatModelDeployment,
                name: "AiFoundryAgent",
                description: "An agent that uses Microsoft AI Foundry capabilities to answer questions.",
                instructions: "You are an AI assistant that helps people find information.",
                tools: [bingGroundingTool]
            );
        }
        else
        {
            sk_ai_agent_definition = await aiagents_client.Administration.GetAgentAsync(aifoundryagent_id);
        }

        sk_aifoundry_agent = new AzureAIAgent(sk_ai_agent_definition, aiagents_client);

        Console.WriteLine($"\n\n=========== Agent <{sk_aifoundry_agent.Name}> was initialized with id <{sk_aifoundry_agent.Id}> ===========\n\n");
    }
}