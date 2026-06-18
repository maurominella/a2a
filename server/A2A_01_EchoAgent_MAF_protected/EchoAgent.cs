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

        // Streaming "vero": invece di restituire tutto il testo in un unico update,
        // lo spezziamo in piu' chunk ed emettiamo un ChatResponseUpdate per ciascuno.
        // Cosi' il client riceve piu' eventi SSE, indipendentemente dalla lunghezza.
        const int chunkSize = 64; // caratteri per chunk (regolabile)

        if (text.Length == 0)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, string.Empty);
            yield break;
        }

        for (int i = 0; i < text.Length; i += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunk = text.Substring(i, Math.Min(chunkSize, text.Length - i));
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);

            // Cede il controllo cosi' ogni chunk puo' essere serializzato/inviato
            // separatamente invece di essere bufferizzato in un'unica risposta.
            await Task.Yield();
        }
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