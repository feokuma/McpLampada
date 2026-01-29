using McpLampada.Services;

var builder = WebApplication.CreateBuilder(args);

// Registrar dependências
builder.Services.AddSingleton<McpLampada.Controllers.LampadaController>(sp => new(pin: 2));
builder.Services.AddSingleton<McpService>();

// Configurar servidor MCP
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() 
        { 
            Name = "lampada_dotnet",   
            Title = "Controle de Lâmpada via MCP",
            Description = "Servidor MCP para controlar uma lâmpada conectada ao GPIO.",
            Version = "1.0.0",
        };
    })
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly();

var app = builder.Build();

// Mapear endpoint MCP
app.MapMcp("/mcp");

await app.RunAsync();
