using McpLampada.Services;

var builder = WebApplication.CreateBuilder(args);

// Registrar dependências
builder.Services.AddSingleton<McpLampada.Controllers.LampadaController>(sp => new(pin: 2));
builder.Services.AddSingleton<McpService>();

// Configurar servidor MCP
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Mapear endpoint MCP
app.MapMcp("/mcp");

await app.RunAsync();
