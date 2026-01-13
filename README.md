# McpLampada

MCP Server em .NET que controla uma lâmpada (GPIO) em uma Raspberry Pi, acessível via rede HTTP. A execução acontece sempre na Raspberry Pi, mas a compilação/publicação é feita fora da Raspberry Pi (em uma máquina de desenvolvimento).

## Requisitos
- Máquina de desenvolvimento com .NET 10.0 SDK para publicar os binários.
- Raspberry Pi com .NET 10.0 Runtime instalado.
- Raspberry Pi com pino GPIO disponível (exemplo usa GPIO2 / pino físico 3).
- Rede acessível para o VS Code/GitHub Copilot se conectar à Pi.

## Publicar em máquina de desenvolvimento (cross-targeting ARM)
Gere os artefatos para a arquitetura da sua Raspberry Pi usando os RIDs:

```bash
# ARM64 (Pi 4/5 64-bit OS)
dotnet publish -c Release -r linux-arm64 --self-contained false

# ARM32 (Pi 3 ou Raspberry Pi OS 32-bit)
dotnet publish -c Release -r linux-arm --self-contained false
```

Os artefatos vão para `bin/Release/net10.0/<rid>/publish/`. Com `--self-contained false`, o .NET Runtime precisa estar instalado na Raspberry Pi.

## Transferir para a Raspberry Pi
Após publicar, transfira a pasta `publish/` para a Raspberry Pi (por exemplo, via SCP). Consulte o guia detalhado em [DEPLOY_SCP.md](DEPLOY_SCP.md).

## Executar na Raspberry Pi
Na Raspberry Pi, dentro da pasta publicada, execute o binário permitindo acesso de rede:

```bash
./McpLampada --urls "http://0.0.0.0:5000"
```
ou
```bash
dotnet McpLampada.dll --urls "http://0.0.0.0:5000"
```

Por que `0.0.0.0`? Isso faz o servidor (Kestrel) escutar em todas as interfaces de rede da Raspberry Pi. Se você usar `localhost`/`127.0.0.1`, o serviço só será acessível localmente na própria Pi e o VS Code em outra máquina não conseguirá conectar. Com `0.0.0.0`, dispositivos na mesma rede podem acessar `http://<ip-da-pi>:5000/mcp`. Considere restringir o acesso via firewall/VLAN quando necessário.

O servidor MCP estará disponível em `http://<ip-da-pi>:5000/mcp`.

## Permissões de GPIO na Raspberry Pi
Para acessar GPIO sem `sudo`:
```bash
sudo usermod -a -G gpio $USER
# faça logout/login após alterar o grupo
```

## Integração com GitHub Copilot (VS Code)
O servidor roda na Raspberry Pi e o VS Code se conecta via HTTP pela rede. Para adicionar o servidor MCP ao Copilot via Paleta de Comandos:

1) Certifique-se de que o servidor está rodando na Raspberry Pi na porta 5000.

2) No VS Code, pressione `Ctrl + P` e digite: `MCP: Add Server...` (ou selecione no menu de comandos).

3) Escolha o tipo de servidor HTTP e informe:
   - Nome: `lampada` (opcional)
   - URL do endpoint: `http://<ip-da-pi>:5000/mcp`

4) Confirme e finalize. No Copilot Chat, o servidor "lampada" aparecerá com as ferramentas `ligar_lampada`, `desligar_lampada` e `status_lampada`.

## Testar manualmente via HTTP
Você também pode testar o MCP server diretamente com `curl` (a partir de qualquer máquina na mesma rede, usando o IP da Pi):

```bash
# Initialize
curl -X POST http://<ip-da-pi>:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}'

# Listar ferramentas
curl -X POST http://<ip-da-pi>:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

# Ligar lâmpada
curl -X POST http://<ip-da-pi>:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"ligar_lampada"}}'

# Verificar status
curl -X POST http://<ip-da-pi>:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"status_lampada"}}'
```

Opcional: com a extensão REST Client no VS Code, você pode editar o IP em [test.http](test.http) e enviar as requisições.

## Segurança
Este é um exemplo minimalista; não há autenticação ou isolamento. Restrinja o acesso físico/rede conforme necessário e considere executar atrás de uma VLAN ou firewall.

### Restringir acesso à porta 5000
Se você expõe `http://0.0.0.0:5000`, recomenda-se limitar quem pode acessar a porta 5000.

#### Usando UFW (mais simples)
```bash
# Instalar (se necessário)
sudo apt-get update && sudo apt-get install -y ufw

# Permitir apenas da máquina de desenvolvimento (substitua <dev-ip>)
sudo ufw allow from <dev-ip> to any port 5000 proto tcp

# Alternativa: permitir da sub-rede local (ex.: 192.168.1.0/24)
sudo ufw allow from 192.168.1.0/24 to any port 5000 proto tcp

# Bloquear o restante (padrão: deny incoming)
sudo ufw default deny incoming

# Ativar e verificar
sudo ufw enable
sudo ufw status verbose
```

Observações:
- Ao ativar o UFW, revise regras existentes para não interromper SSH (garanta `sudo ufw allow ssh`).
- Ajuste o intervalo da sub-rede conforme sua rede local.

#### Usando iptables (mais flexível)
```bash
# Permitir apenas da máquina de desenvolvimento
sudo iptables -A INPUT -p tcp --dport 5000 -s <dev-ip> -j ACCEPT

# (opcional) Permitir da sub-rede inteira
sudo iptables -A INPUT -p tcp --dport 5000 -s 192.168.1.0/24 -j ACCEPT

# Bloquear todo o restante para a porta 5000
sudo iptables -A INPUT -p tcp --dport 5000 -j DROP

# Tornar persistente (Debian/Raspberry Pi OS)
sudo apt-get install -y netfilter-persistent
sudo netfilter-persistent save
```

Teste o acesso a partir da sua máquina de desenvolvimento e confirme que outros hosts não conseguem abrir `http://<ip-da-pi>:5000/mcp`.
