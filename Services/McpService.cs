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
            },
            new JsonObject
            {
                ["name"] = "status_lampada",
                ["description"] = "Retorna se a lâmpada está ligada ou desligada.",
                ["inputSchema"] = noArgsSchema.DeepClone()
            }
        };

        var result = new JsonObject
        {
            ["tools"] = tools
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
            case "status_lampada":
                bool ligada = _lampada.Status();
                statusText = ligada ? "A lâmpada está ligada." : "A lâmpada está desligada.";
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