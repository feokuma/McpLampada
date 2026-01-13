using System.ComponentModel;
using McpLampada.Controllers;
using ModelContextProtocol.Server;

namespace McpLampada.Services;

[McpServerToolType]
public class McpService
{
    private readonly LampadaController _lampada;

    public McpService(LampadaController lampada)
    {
        _lampada = lampada;
    }

    [McpServerTool, Description("Liga a lâmpada conectada ao pino GPIO configurado.")]
    public string LigarLampada()
    {
        _lampada.Ligar();
        return "Lâmpada ligada com sucesso.";
    }

    [McpServerTool, Description("Desliga a lâmpada conectada ao pino GPIO configurado.")]
    public string DesligarLampada()
    {
        _lampada.Desligar();
        return "Lâmpada desligada com sucesso.";
    }

    [McpServerTool, Description("Retorna se a lâmpada está ligada ou desligada.")]
    public string StatusLampada()
    {
        bool ligada = _lampada.Status();
        return ligada ? "A lâmpada está ligada." : "A lâmpada está desligada.";
    }
}