using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentServer;

// "Cervello" dell'agente: restituisce esattamente il testo inviato dall'utente.
internal sealed class EchoChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var text = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
        yield return new ChatResponseUpdate(ChatRole.Assistant, text);
        await Task.CompletedTask;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}

// L'agente MAF esposto via A2A.
public static class EchoAgent
{
    public static AIAgent Create() =>
        new EchoChatClient().AsAIAgent(
            name: "Echo Agent",
            description: "Echoes messages back to the user");
}