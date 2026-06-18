using AgentServer;
using Microsoft.Agents.AI;

var builder = WebApplication.CreateBuilder(args);

// Crea l'agente MAF "Echo" e registra il server A2A per esso (richiesto da MapA2AJsonRpc)
AIAgent echoAgent = EchoAgent.Create();
builder.Services.AddA2AServer(echoAgent);

var app = builder.Build();

// Espone l'agente via A2A, NON autenticato (nessun RequireAuthorization):
// - binding JSON-RPC (usato dai client A2A "nativi") sulla root "/"
app.MapA2AJsonRpc(echoAgent, "/");
// - binding HTTP+JSON (REST), comodo per test da browser/curl, sotto "/a2a"
app.MapA2AHttpJson(echoAgent, "/a2a");

app.Run();