// Copyright (c) Microsoft. All rights reserved.

#region Libraries and Namespaces
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Agents; // needed for ChatCompletion
using AIPlugins; // contains the LightsPlugin class
using A2A; // contains the AISettings class
using LLMSettings;
namespace SKAgentNamespace; // This workspace, that contains the SKCompletionAgent class
#endregion

public class SKCompletionAgent
{
    ChatCompletionAgent? _agent;

    public SKCompletionAgent()
    {
        // Initialize the agent here if needed
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
            Name = "SK Agent",
            Description = "SK Agent using Semantic Kernel Chat Completion",
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

        // create and return an artifact\
        var message = new A2A.AgentMessage()
        {
            Role = MessageRole.Agent,
            MessageId = Guid.NewGuid().ToString(),
            ContextId = messageSendParams.Message.ContextId,
            Parts = [new TextPart { Text = $"Response:\n{response}" }]
        };

        return Task.FromResult<A2A.A2AResponse>(message);
    }

    
    private void InitializeAgent()
    {
        #region Environment Configuration
        // Load configuration from environment variables or user secrets.
        var ai_settings = new AISettings();
        #endregion

        // Create the kernel builder with the pointer to Azure OpenAI
        var kernelBuilder = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                ai_settings.AzureOpenAI.ChatModelDeployment,
                ai_settings.AzureOpenAI.Endpoint,
                ai_settings.AzureOpenAI.ApiKey
            );

        // Build the kernel
        Kernel kernel = kernelBuilder.Build();

        var azureOpenAIPromptExecutionSettings = new AzureOpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
        var kernelArguments = new KernelArguments(azureOpenAIPromptExecutionSettings) // optional            
        {
            { "repository", "microsoft/semantic-kernel" }
        };

        string agent_name = "sk_chatcompletion_agent";
        string instructions = "you are a clever agent";

        _agent = new ChatCompletionAgent
        {
            Name = agent_name,
            Instructions = instructions,
            Kernel = kernel,
            Arguments = kernelArguments ?? new KernelArguments() // Provide a default value if kernelArguments is null
        };

        var pluginInstance = new LightsPlugin();
        kernel.Plugins.AddFromObject(pluginInstance, "Lights");

        Console.WriteLine($"\n\n=========== Agent <{agent_name}> was initialized ===========\n\n");
    }
    
    private async Task<string> GenericChatWithAgentAsync(object? agent, string? question = null)
    {
        string? agent_response = "";

        if (agent is ChatCompletionAgent sk_chatcompletion_agent)
        {
            var sk_chatcompletionagent_thread = new ChatHistoryAgentThread();
            var message = new ChatMessageContent(AuthorRole.User, question);

            Console.WriteLine("\n");

            // await foreach (StreamingChatMessageContent response in sk_chatcompletion_agent.InvokeStreamingAsync(message: message, thread: sk_chatcompletionagent_thread))
            await foreach (ChatMessageContent response in sk_chatcompletion_agent.InvokeAsync(message: message, thread: sk_chatcompletionagent_thread))
            {
                Console.Write(response.Content);
                agent_response += response.Content;
            }
            Console.WriteLine("\n\n+++++++++++++++++\n");
        }
        else
        {
            throw new ArgumentException("The provided agent is not a ChatCompletionAgent.");
        }

        return agent_response;
    }
}