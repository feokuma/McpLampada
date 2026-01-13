# Deploy via SCP para Raspberry Pi

Este guia mostra como copiar os artefatos publicados do McpLampada para a Raspberry Pi usando `scp`.

## Pré-requisitos
- Raspberry Pi com SSH habilitado (`sudo raspi-config` → Interface Options → SSH).
- Usuário e senha/chave de acesso ao SSH (por padrão, usuário costuma ser `pi`).
- Artefatos publicados no seu computador: `bin/Release/net10.0/<rid>/publish/`.

## Descobrir o IP da Raspberry Pi
Na própria Pi:
```bash
hostname -I
```
Ou pelo seu computador, se mDNS estiver habilitado:
```bash
ping raspberrypi.local
```

## Copiar usando scp (diretório publish)
No computador de desenvolvimento, substitua `<ip-da-pi>` e ajuste o caminho de destino conforme necessário:
```bash
# ARM64 (exemplo de pasta)
scp -r bin/Release/net10.0/linux-arm64/publish pi@<ip-da-pi>:/home/pi/McpLampada

# ARM32 (exemplo de pasta)
scp -r bin/Release/net10.0/linux-arm/publish pi@<ip-da-pi>:/home/pi/McpLampada
```

Se usar porta SSH diferente de 22:
```bash
scp -r -P 2222 bin/Release/net10.0/linux-arm64/publish pi@<ip-da-pi>:/home/pi/McpLampada
```

## Permissões e execução no destino
Na Raspberry Pi:
```bash
cd /home/pi/McpLampada/publish
chmod +x McpLampada
./McpLampada --urls "http://0.0.0.0:5000"
```

## Dicas
- Para chaves SSH: adicione `-i /caminho/para/sua/chave` ao comando `scp`.
- Para transferência incremental, use `rsync` (se disponível) ao invés de `scp`.
- Garanta que o .NET Runtime compatível esteja instalado na Raspberry Pi quando publicar com `--self-contained false`.
