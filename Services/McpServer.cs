using System.Text.Json;
using System.Text.Json.Nodes;
using McpLampada.Tools;

namespace McpLampada;

public class McpServer
{
    private readonly LampadaTool _lampadaTool = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
    private bool _initialized;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var input = await Console.In.ReadLineAsync();
            if (input is null) break; // stdin fechado
            if (string.IsNullOrWhiteSpace(input)) continue;

            JsonNode? message;
            try
            {
                message = JsonNode.Parse(input);
            }
            catch (Exception ex)
            {
                WriteErrorResponse(null, "invalid_json", ex.Message);
                continue;
            }

            var method = message?["method"]?.ToString();
            var id = message?["id"];
            var parameters = message?["params"] as JsonObject ?? new JsonObject();

            if (string.IsNullOrWhiteSpace(method))
            {
                WriteErrorResponse(id, "invalid_request", "Campo 'method' é obrigatório.");
                continue;
            }

            try
            {
                switch (method)
                {
                    case "initialize":
                        HandleInitialize(id);
                        break;
                    case "tools/list":
                        HandleToolsList(id);
                        break;
                    case "tools/call":
                        HandleToolsCall(id, parameters);
                        break;
                    default:
                        WriteErrorResponse(id, "method_not_found", $"Método '{method}' não é suportado.");
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteErrorResponse(id, "internal_error", ex.Message);
            }
        }
    }

    private void HandleInitialize(JsonNode? id)
    {
        _initialized = true;

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

        WriteResponse(id, result);
    }

    private void HandleToolsList(JsonNode? id)
    {
        if (!_initialized)
        {
            WriteErrorResponse(id, "not_initialized", "Chame 'initialize' antes de listar ferramentas.");
            return;
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
                ["description"] = "Liga a lâmpada conectada ao pino configurado.",
                ["inputSchema"] = noArgsSchema.DeepClone()
            },
            new JsonObject
            {
                ["name"] = "desligar_lampada",
                ["description"] = "Desliga a lâmpada conectada ao pino configurado.",
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
            ["tools"] = tools,
            ["nextCursor"] = null
        };

        WriteResponse(id, result);
    }

    private void HandleToolsCall(JsonNode? id, JsonObject parameters)
    {
        if (!_initialized)
        {
            WriteErrorResponse(id, "not_initialized", "Chame 'initialize' antes de executar ferramentas.");
            return;
        }

        var name = parameters["name"]?.ToString();
        if (string.IsNullOrWhiteSpace(name))
        {
            WriteErrorResponse(id, "invalid_params", "Parâmetro 'name' é obrigatório.");
            return;
        }

        JsonNode? toolResult = name switch
        {
            "ligar_lampada" => _lampadaTool.Ligar(),
            "desligar_lampada" => _lampadaTool.Desligar(),
            "status_lampada" => _lampadaTool.Status(),
            _ => JsonNode.Parse("{\"error\":\"Ferramenta não encontrada\"}")
        };

        if (toolResult?["error"] is not null)
        {
            WriteErrorResponse(id, "tool_not_found", "Ferramenta solicitada não existe.");
            return;
        }

        var result = new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = toolResult?.ToJsonString() ?? "{}"
                }
            }
        };

        WriteResponse(id, result);
    }

    private void WriteResponse(JsonNode? id, JsonObject result)
    {
        var response = new JsonObject
        {
            ["type"] = "response",
            ["id"] = id?.DeepClone(),
            ["result"] = result
        };

        Console.WriteLine(response.ToJsonString(_jsonOptions));
    }

    private void WriteErrorResponse(JsonNode? id, string code, string message)
    {
        var error = new JsonObject
        {
            ["code"] = code,
            ["message"] = message
        };

        var response = new JsonObject
        {
            ["type"] = "error",
            ["id"] = id?.DeepClone(),
            ["error"] = error
        };

        Console.WriteLine(response.ToJsonString(_jsonOptions));
    }
}
