# Optimizer 0.1.0-preview

Primeira versão de diagnóstico para PCs de jogos com **Windows 10 atualizado ou Windows 11, x64 (Intel/AMD)**.

## Baixar e abrir

1. Em **Assets**, baixe **Optimizer-windows-x64.zip**. Os arquivos “Source code” são o código do projeto, não o aplicativo pronto.
2. Clique no ZIP com o botão direito e escolha **Extrair tudo**.
3. Abra a pasta extraída e dê dois cliques em **Optimizer.App.exe**.

O pacote inclui o .NET. Não precisa instalar SDK, Visual Studio ou ferramentas de programação. Mantenha o executável junto de todos os arquivos extraídos.

## O que esta versão faz

- Identifica processador, placas de vídeo e sistema operacional.
- Mostra uso de CPU e memória com atualização automática a cada 2 segundos.
- Lista até oito processos com maior consumo de CPU e memória.
- Apresenta pontos de atenção para investigar o consumo durante os jogos.

Aguarde duas leituras para avaliar CPU. Fechar a janela encerra o aplicativo. O diagnóstico é local, não exige administrador e não altera configurações do Windows.

## Estado de validação

O fluxo de publicação exige build, testes de lógica e testes de contadores reais no Windows aprovados antes de disponibilizar o pacote. Também abre a janela real, verifica porcentagens de CPU/memória pela automação de interface e confirma o encerramento ao fechar. A aparência visual e o comportamento durante jogos ainda precisam de validação em um PC Windows.

Esta versão **não aplica otimizações, não mede FPS, 1% low ou input lag e não promete ganhos de desempenho**. Temperatura e uso da GPU ainda não são coletados. Em máquinas com mais de 64 processadores lógicos, a leitura global de CPU tem a limitação de grupos documentada no README.

O arquivo **Optimizer-windows-x64.zip.sha256** permite conferir a integridade do download.
