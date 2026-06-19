using A2A;
using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;

// ────────────────────────────────────────────────────────────────────────────────────────────
// Client A2A per l'agente "Echo" (server A2A_01_EchoAgent_MAF_protected).
// Il server espone il binding JSON-RPC sulla root "/", protetto con Entra ID.
// Acquisiamo un token Entra ID e lo alleghiamo come Bearer.
//
// Due modalita' di autenticazione:
//   - utente (default): identita' di "az login", token DELEGATO (scp=access_as_user).
//   - applicazione (--app): service principal delle variabili AZURE_*, token
//     APP-ONLY (roles=Agent.Invoke) via client credentials. Pattern machine-to-machine.
// ────────────────────────────────────────────────────────────────────────────────────────────

// Flag "--app" => modalita' applicazione; gli altri argomenti sono posizionali.
bool appMode = args.Any(a => a.Equals("--app", StringComparison.OrdinalIgnoreCase));
var positional = args.Where(a => !a.StartsWith("--")).ToArray();

// Endpoint del server. Default: la porta http del nostro Web API (launchSettings).
var baseUrl = new Uri(positional.Length > 0 ? positional[0] : "https://giant-shoe-9mrg4pr.euw.devtunnels.ms/"); // "http://localhost:5285/", "https://f655ncg6-5285.euw.devtunnels.ms"

// Scope dell'API protetta (App ID URI del server + "/.default").
// Override possibile via secondo argomento posizionale.
var apiScope = positional.Length > 1
    ? positional[1]
    : "api://211b26e8-ca58-4150-8989-b7c608931ed9/.default";

using var http = new HttpClient();

// Acquisizione del token Entra ID.
// - modalita' applicazione: EnvironmentCredential (AZURE_CLIENT_ID/SECRET/TENANT) -> token app-only.
// - modalita' utente: escludiamo la EnvironmentCredential per usare Azure CLI ("az login").
Console.WriteLine($"Acquisizione del token Entra ID (modalita': {(appMode ? "applicazione" : "utente")})...");
try
{
    TokenCredential credential = appMode
        ? new EnvironmentCredential()
        : new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = true
        });
    AccessToken token = await credential.GetTokenAsync(
        new TokenRequestContext(new[] { apiScope }), CancellationToken.None);
    http.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token.Token);
    Console.WriteLine($"Token acquisito (scade: {token.ExpiresOn.ToLocalTime():HH:mm:ss}).");

    // DIAGNOSTICA: decodifica i claim del token (NON stampa il token).
    PrintTokenClaims(token.Token);
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine($"Impossibile acquisire il token: {ex.Message}");
    Console.WriteLine("Assicurati di aver eseguito 'az login' e di avere accesso allo scope.");
    return;
}

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

// ─── Helper diagnostico: decodifica e stampa alcuni claim del JWT ───
static void PrintTokenClaims(string jwt)
{
    try
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) { Console.WriteLine("  (token non in formato JWT)"); return; }
        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        string Get(string n) => root.TryGetProperty(n, out var v) ? v.ToString() : "(assente)";
        Console.WriteLine($"  aud={Get("aud")}  tid={Get("tid")}");
        Console.WriteLine($"  scp={Get("scp")}  appid={Get("appid")}  upn={Get("upn")}");
    }
    catch (Exception ex) { Console.WriteLine($"  (decodifica claim fallita: {ex.Message})"); }
}
