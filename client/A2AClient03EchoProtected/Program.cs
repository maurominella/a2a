using A2A;

// ─────────────────────────────────────────────────────────────────────────────
// Client A2A per l'agente "Echo" (server A2A_01_EchoAgent_MAF_protected).
// Il server espone il binding JSON-RPC sulla root "/" (NON autenticato).
// ─────────────────────────────────────────────────────────────────────────────

// Endpoint del server. Default: la porta http del nostro Web API (launchSettings).
var baseUrl = new Uri(args.Length > 0 ? args[0] : "http://localhost:5285/");

using var http = new HttpClient();

// (Opzionale) Recupera e mostra la "agent card", se il server espone il binding
// HTTP+JSON su /a2a (GET /a2a/card). Se non disponibile, proseguiamo comunque.
try
{
    var resolver = new A2ACardResolver(baseUrl, http, agentCardPath: "/a2a/card");
    AgentCard card = await resolver.GetAgentCardAsync();
    Console.WriteLine($"Connesso all'agente: \"{card.Name}\" — {card.Description}");
}
catch (Exception ex)
{
    Console.WriteLine($"(Agent card non disponibile: {ex.Message})");
}

// Client A2A: parla in JSON-RPC con l'endpoint sulla root "/".
var client = new A2AClient(baseUrl, http);

Console.WriteLine($"Server: {baseUrl}");
Console.WriteLine("Scrivi un messaggio e premi Invio. Riga vuota per uscire.");
Console.WriteLine("Prefisso 'stream:' per ricevere la risposta in streaming.\n");

while (true)
{
    Console.Write("Tu  > ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question))
        break;

    // Streaming se l'utente scrive "stream: ..."
    if (question.StartsWith("stream:", StringComparison.OrdinalIgnoreCase))
    {
        var text = question["stream:".Length..].Trim();
        Console.Write("Eco > ");
        await foreach (StreamResponse ev in client.SendStreamingMessageAsync(text, Role.User, contextId: null))
        {
            var chunk = ev.Message?.Parts?.FirstOrDefault()?.Text;
            if (!string.IsNullOrEmpty(chunk))
                Console.Write(chunk);
        }
        Console.WriteLine();
        continue;
    }

    // Invio non-streaming.
    SendMessageResponse response = await client.SendMessageAsync(question, Role.User, contextId: null);
    var answer = response.Message?.Parts?.FirstOrDefault()?.Text;
    Console.WriteLine($"Eco > {answer}");
}

Console.WriteLine("Arrivederci!");
