using LLMSettings;
using Microsoft.Agents.CopilotStudio.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Copilot;

namespace ConnectCopilotStudio

{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var ai_settings = new AISettings();
            Console.WriteLine("Hello, Copilot Studio Agents!");
            string tenantId = ai_settings.GetVariable("ConnectCopilotStudioTenantId");
            string appClientId = ai_settings.GetVariable("ConnectCopilotStudioAppClientId");
            string appClientSecret = ai_settings.GetVariable("ConnectCopilotStudioAgentSecret");


            var copilotStudioConnectionSettings = new CopilotStudioConnectionSettings(
                tenantId: tenantId,
                appClientId: appClientId,
                appClientSecret: appClientSecret);

            copilotStudioConnectionSettings.EnvironmentId = ai_settings.GetVariable("ConnectCopilotStudioEnvironmentId");
            copilotStudioConnectionSettings.SchemaName = ai_settings.GetVariable("ConnectCopilotStudioSchemaName");

            CopilotClient copilotClient = CopilotStudioAgent.CreateClient(copilotStudioConnectionSettings);
            CopilotStudioAgent copilotStudioAgent = new CopilotStudioAgent(copilotClient);


            await foreach (ChatMessageContent chatMessage in copilotStudioAgent.InvokeAsync(
                "What's the warranty coverage for the TrailMaster X4 Tent?"))
            {
                Console.Write(chatMessage.Content);
            }
        }
    }
}