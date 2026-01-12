using System.Text.Json.Nodes;
using McpLampada.Controllers;

namespace McpLampada.Tools;

public class LampadaTool
{
    private readonly LampadaController _lampada = new(pin: 2); // GPIO2 (PIN 3 da placa)

    public JsonNode Ligar()
    {
        _lampada.Ligar();
        return JsonNode.Parse("{\"status\":\"ligada\"}");
    }

    public JsonNode Desligar()
    {
        _lampada.Desligar();
        return JsonNode.Parse("{\"status\":\"desligada\"}");
    }

    public JsonNode Status()
    {
        bool ligada = _lampada.Status();
        return JsonNode.Parse("{\"status\":\"" + (ligada ? "ligada" : "desligada") + "\"}");
    }
}
