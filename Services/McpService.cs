using System.Text.Json;
using System.Text.Json.Nodes;
using McpLampada.Controllers;

namespace McpLampada.Services;

public class McpService
{
    private readonly LampadaController _lampada;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private bool _initialized;
    private string? _sessionId;

    public McpService(LampadaController lampada)
    {
        _lampada = lampada;
    }

    public async Task<string> HandleMessageAsync(string messageJson)
    {
        JsonNode? message;
        try
        {
            message = JsonNode.Parse(messageJson);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(null, "invalid_json", ex.Message);
        }

        var method = message?["method"]?.ToString();
        var id = message?["id"];
        var parameters = message?["params"] as JsonObject ?? new JsonObject();

        if (string.IsNullOrWhiteSpace(method))
        {
            return CreateErrorResponse(id, "invalid_request", "Campo 'method' é obrigatório.");
        }

        try
        {
            return method switch
            {
                "initialize" => HandleInitialize(id),
                "tools/list" => HandleToolsList(id),
                "tools/call" => HandleToolsCall(id, parameters),
                "resources/list" => HandleResourcesList(id),
                "resources/read" => HandleResourcesRead(id, parameters),
                _ => CreateErrorResponse(id, "method_not_found", $"Método '{method}' não é suportado.")
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(id, "internal_error", ex.Message);
        }
    }

    private string HandleInitialize(JsonNode? id)
    {
        _initialized = true;
        _sessionId = Guid.NewGuid().ToString("N");

        var result = new JsonObject
        {
            ["protocolVersion"] = "1.0",
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "lampada_dotnet",
                ["version"] = "1.0.0"
            },
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject
                {
                    ["listChanged"] = true
                },
                ["resources"] = new JsonObject
                {
                    ["subscribe"] = false,
                    ["listChanged"] = false
                }
            }
        };

        return CreateResponse(id, result);
    }

    public void HandleSessionClose(string? sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId) && sessionId == _sessionId)
        {
            _initialized = false;
            _sessionId = null;
            // Cleanup: desligar lâmpada ao fechar sessão
            _lampada.Desligar();
        }
    }

    private string HandleToolsList(JsonNode? id)
    {
        if (!_initialized)
        {
            return CreateErrorResponse(id, "not_initialized", "Chame 'initialize' antes de listar ferramentas.");
        }

        var noArgsSchema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject(),
            ["required"] = new JsonArray(),
            ["additionalProperties"] = false
        };

        var tools = new JsonArray
        {
            new JsonObject
            {
                ["name"] = "ligar_lampada",
                ["description"] = "Liga a lâmpada conectada ao pino GPIO configurado.",
                ["inputSchema"] = noArgsSchema.DeepClone()
            },
            new JsonObject
            {
                ["name"] = "desligar_lampada",
                ["description"] = "Desliga a lâmpada conectada ao pino GPIO configurado.",
                ["inputSchema"] = noArgsSchema.DeepClone()
            }
        };

        var result = new JsonObject
        {
            ["tools"] = tools
        };

        return CreateResponse(id, result);
    }

    private string HandleResourcesList(JsonNode? id)
    {
        if (!_initialized)
        {
            return CreateErrorResponse(id, "not_initialized", "Chame 'initialize' antes de listar resources.");
        }

        var resources = new JsonArray
        {
            new JsonObject
            {
                ["uri"] = "lampada://status",
                ["name"] = "Status da Lâmpada",
                ["description"] = "Estado atual da lâmpada (ligada ou desligada)",
                ["mimeType"] = "text/plain"
            }
        };

        var result = new JsonObject
        {
            ["resources"] = resources
        };

        return CreateResponse(id, result);
    }

    private string HandleResourcesRead(JsonNode? id, JsonObject parameters)
    {
        if (!_initialized)
        {
            return CreateErrorResponse(id, "not_initialized", "Chame 'initialize' antes de ler resources.");
        }

        var uri = parameters["uri"]?.ToString();
        if (string.IsNullOrWhiteSpace(uri))
        {
            return CreateErrorResponse(id, "invalid_params", "Parâmetro 'uri' é obrigatório.");
        }

        if (uri != "lampada://status")
        {
            return CreateErrorResponse(id, "resource_not_found", $"Resource '{uri}' não existe.");
        }

        bool ligada = _lampada.Status();
        string statusText = ligada ? "ligada" : "desligada";

        var result = new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["uri"] = "lampada://status",
                    ["mimeType"] = "text/plain",
                    ["text"] = statusText
                }
            }
        };

        return CreateResponse(id, result);
    }

    private string HandleToolsCall(JsonNode? id, JsonObject parameters)
    {
        if (!_initialized)
        {
            return CreateErrorResponse(id, "not_initialized", "Chame 'initialize' antes de executar ferramentas.");
        }

        var name = parameters["name"]?.ToString();
        if (string.IsNullOrWhiteSpace(name))
        {
            return CreateErrorResponse(id, "invalid_params", "Parâmetro 'name' é obrigatório.");
        }

        string statusText;
        switch (name)
        {
            case "ligar_lampada":
                _lampada.Ligar();
                statusText = "Lâmpada ligada com sucesso.";
                break;
            case "desligar_lampada":
                _lampada.Desligar();
                statusText = "Lâmpada desligada com sucesso.";
                break;
            default:
                return CreateErrorResponse(id, "tool_not_found", "Ferramenta solicitada não existe.");
        }

        var result = new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = statusText
                }
            }
        };

        return CreateResponse(id, result);
    }

    private string CreateResponse(JsonNode? id, JsonObject result)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["result"] = result
        };

        return response.ToJsonString(_jsonOptions);
    }

    private string CreateErrorResponse(JsonNode? id, string code, string message)
    {
        var error = new JsonObject
        {
            ["code"] = code,
            ["message"] = message
        };

        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = error
        };

        return response.ToJsonString(_jsonOptions);
    }
}