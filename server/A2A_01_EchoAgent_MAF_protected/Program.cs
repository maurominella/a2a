/* https://learn.microsoft.com/en-us/azure/ai-services/agents/quickstart?pivots=rest-api
az login --use-device-code --tenant 3ad0b905-34ab-4116-93d9-c1dcc2d35af6
APP_OBJ_ID=128bb5ae-9801-4e7e-80f9-e4d814035e37; echo $APP_OBJ_ID
APP_ID_URI=$(az ad app show --id $APP_OBJ_ID --query "identifierUris[0]" -o tsv | tr -d '\r'); echo $APP_ID_URI
SCOPE_VALUE=$(az ad app show --id $APP_OBJ_ID --query "api.oauth2PermissionScopes[0].value" -o tsv | tr -d '\r'); echo $SCOPE_VALUE
TOKEN=$(az account get-access-token --scope "${APP_ID_URI}/${SCOPE_VALUE}" --query accessToken -o tsv); echo $TOKEN

devtunnel host -p 5285 --allow-anonymous
devtunnel delete puzzled-field-99w1zj0
*/ 

using AgentServer;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Crea l'agente MAF "Echo" e registra il server A2A per esso (richiesto da MapA2AJsonRpc)
AIAgent echoAgent = EchoAgent.Create();
builder.Services.AddA2AServer(echoAgent);

// Autenticazione: valida i token JWT Bearer emessi da Microsoft Entra ID
// usando i parametri della sezione "AzureAd" di appsettings.json.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Autorizzazione: abilita i criteri (li applicheremo agli endpoint nel passo 4).
builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware: prima autentica (chi sei), poi autorizza (cosa puoi fare).
app.UseAuthentication();
app.UseAuthorization();

// Espone l'agente via A2A, PROTETTO da Microsoft Entra ID:
// ogni chiamata senza un token Bearer valido riceve 401 Unauthorized.
// - binding JSON-RPC (usato dai client A2A "nativi") sulla root "/"
app.MapA2AJsonRpc(echoAgent, "/").RequireAuthorization();
// - binding HTTP+JSON (REST), comodo per test da browser/curl, sotto "/a2a"
app.MapA2AHttpJson(echoAgent, "/a2a").RequireAuthorization();

app.Run();