// Copyright (c) Microsoft. All rights reserved.

// **Documentation**: [`ChatHistoryAgentThread`](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/examples/example-chat-agent?pivots=programming-language-csharp)


#region Libraries and Namespaces
// dotnet add package Microsoft.Extensions.Logging --> <PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.9" />
// dotnet add package Microsoft.Extensions.Logging.Console --> <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="9.0.9" />
using Microsoft.Extensions.Logging; // needed for LogLevel

// dotnet add package Microsoft.Extensions.DependencyInjection --> <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.9" />
using Microsoft.Extensions.DependencyInjection; // needed for AddLogging

// dotnet add package Microsoft.SemanticKernel --> <PackageReference Include="Microsoft.SemanticKernel" Version="1.65.0" />
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

// dotnet add package Microsoft.SemanticKernel.Agents.Core --> <PackageReference Include="Microsoft.SemanticKernel.Agents.Core" Version="1.65.0" />
using Microsoft.SemanticKernel.Agents; // needed for ChatCompletion

// dotnet add package DotNetEnv --> <PackageReference Include="DotNetEnv" Version="3.1.1" />
using DotNetEnv;

// dotnet add package Azure.Identity --> <PackageReference Include="Azure.Identity" Version="1.16.0" />
using Azure.Identity;

using AIPlugins; // contains the LightsPlugin class
using LLMSettings; // contains the AISettings class

#endregion

Console.WriteLine("\n+++++++++++++++++ Application starts +++++++++++++++++");

#region Environment Configuration
Console.WriteLine("\n\n\n+++++++++++++++++ Environment Configuration +++++++++++++++++\n");
// Load configuration from environment variables or user secrets.
var ai_settings = new AISettings();
Console.WriteLine($"AZURE_OPENAI_ENDPOINT: {ai_settings.AzureOpenAI.Endpoint}\n" +
$"AZURE_OPENAI_CHAT_DEPLOYMENT_NAME: {ai_settings.AzureOpenAI.ChatModelDeployment}\n" +
$"PROJECT_ENDPOINT: {ai_settings.AzureOpenAI.ProjectEndpoint}\n");
#endregion

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Information));

// Create the kernel builder with the pointer to Azure OpenAI
var kernelBuilder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        ai_settings.AzureOpenAI.ChatModelDeployment,
        ai_settings.AzureOpenAI.Endpoint,
        ai_settings.AzureOpenAI.ApiKey
    );

// Build the kernel
Kernel kernel = kernelBuilder.Build();

// add the plugin to the kernel
Microsoft.SemanticKernel.KernelPlugin lights_plugin = kernel.Plugins.AddFromType<LightsPlugin>("Lights");

// Enable planning
// if "pure" OpenAI, please use      OpenAIPromptExecutionSettings
// in Azure OpenAI, we have     AzureOpenAIPromptExecutionSettings
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

Console.WriteLine("\nDefining a chat completion completion Agent...");
var sk_chatcompletion_agent = new ChatCompletionAgent
{
    Name = agent_name,
    Instructions = instructions,
    Kernel = kernel,
    Arguments = kernelArguments ?? new KernelArguments() // Provide a default value if kernelArguments is null
};

Console.WriteLine("...completion Agent is ready.");

// Create a history store the conversation, however use ChatHistoryAgentThread instead of ChatHistory, which is deprecated
var sk_chatcompletionagent_thread = new ChatHistoryAgentThread();

// Create a predefined question to ask the agent
string predefined_question = "how many meters are there in a mile?";  //"How do I turn on the Table Lamp?";
var message = new ChatMessageContent(AuthorRole.User, predefined_question);

await foreach (StreamingChatMessageContent response in sk_chatcompletion_agent.InvokeStreamingAsync(message: message, thread: sk_chatcompletionagent_thread))
{
    Console.Write($"{response.Content}");
}


// ASP.NET Core minimal API to expose the agent via HTTP endpoint
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/TestAgentInteractive", () => Results.Content(@"
    <html>
    <body>
        <form method='post' action='/submit'>
            <div style='width: 100%;'>
                <input type='text' name='userInput'style='width: 100%; box-sizing: border-box;' />
            </div>
            <div style='margin-top: 10px;'>
                <input type='submit' value='Submit' />
            </div>
        </form>
    </body>
    </html>", "text/html"));

app.MapPost("/submit", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var userInput = form["userInput"];

    // Replace 'yourAgentInstance' with your actual agent object
    var response = await GenericChatWithAgentAsync(agent:sk_chatcompletion_agent, userInput);
    var htmlSafeResponse = response?.Replace("\n", "<br/>");

    return Results.Content($@"
        <html>
        <head>
            <meta charset='UTF-8'>
        </head>
        <body>
            <form method='post' action='/submit'>
                <div style='width: 100%;'>
                    <input type='text' name='userInput' value='{userInput}' style='width: 100%; box-sizing: border-box;' />
                </div>
                <div style='margin-top: 10px;'>
                    <input type='submit' value='Submit' />
                </div>
            </form>
            <label>Answer: {htmlSafeResponse}</label>
        </body>
        </html>", "text/html");
});

app.Run();


static async Task<string> GenericChatWithAgentAsync(object agent, string? question = null)
{
    string? agent_response = "";
    if (agent is ChatCompletionAgent sk_chatcompletion_agent)
    {
        var sk_chatcompletionagent_thread = new ChatHistoryAgentThread();
        var message = new ChatMessageContent(AuthorRole.User, question);

        // await foreach (StreamingChatMessageContent response in sk_chatcompletion_agent.InvokeStreamingAsync(message: message, thread: sk_chatcompletionagent_thread))
        await foreach (ChatMessageContent response in sk_chatcompletion_agent.InvokeAsync(message: message, thread: sk_chatcompletionagent_thread))
        {
            Console.Write($"{response.Content}");
            agent_response += response.Content;
        }
    }
    else
    {
        throw new ArgumentException("The provided agent is not a ChatCompletionAgent.");
    }

    return agent_response;
}