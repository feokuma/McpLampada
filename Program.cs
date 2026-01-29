using McpLampada.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<McpLampada.Controllers.LampadaController>(sp => new(pin: 2));
builder.Services.AddSingleton<McpService>();

var app = builder.Build();

// MCP endpoint - POST para mensagens do cliente
app.MapPost("/mcp", async (HttpContext context, McpService mcpService) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var requestBody = await reader.ReadToEndAsync();
    
    var response = await mcpService.HandleMessageAsync(requestBody);
    
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(response);
});

// MCP endpoint - DELETE para encerrar sessão
app.MapDelete("/mcp", (HttpContext context, McpService mcpService) =>
{
    var sessionId = context.Request.Headers["Mcp-Session-Id"].FirstOrDefault();
    mcpService.HandleSessionClose(sessionId);
    
    return Results.Ok(new { message = "Sessão MCP encerrada com sucesso." });
});

await app.RunAsync();
