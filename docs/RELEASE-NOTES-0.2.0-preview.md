# Optimizer 0.2.0-preview

Nova identidade visual para o diagnóstico geral do PC, com preto profundo, painéis com aparência de vidro, reflexos sutis e acentos frios. O foco do produto é a agilidade do Windows no uso diário; desempenho em jogos será um dos resultados a avaliar.

## Baixar e abrir

1. Em **Assets**, baixe **Optimizer-windows-x64.zip**. Os arquivos “Source code” contêm o projeto, não o aplicativo pronto.
2. Clique no ZIP com o botão direito e escolha **Extrair tudo**.
3. Na pasta extraída, abra **Optimizer.App.exe** e mantenha os outros arquivos junto dele.

O pacote é para **Windows 10 atualizado ou Windows 11, x64 (Intel/AMD)** e inclui o .NET. Não precisa instalar SDK, Visual Studio ou ferramentas de programação.

## O que mudou

- Interface escura com camadas de vidro, contornos suaves e contraste pensado para leitura.
- Hierarquia visual renovada para acompanhar CPU, memória, hardware e processos.
- Textos e recomendações orientados ao uso geral do PC.

O diagnóstico continua local, com atualização automática a cada dois segundos. Aguarde duas leituras para avaliar CPU. O aplicativo não exige administrador e não altera configurações do Windows.

## Validação e limites

A publicação exige build, testes de lógica e testes dos contadores reais no Windows. A automação abre a janela, confere os valores de CPU/memória, aciona a atualização manual e confirma o encerramento. Quando a janela permite maximização, esse estado também é verificado. **Optimizer-preview.png**, nos arquivos desta versão, é uma captura da janela em execução no Windows usado pela publicação.

Esta prévia ainda é um diagnóstico: **não executa limpeza, não gerencia a inicialização, não aplica otimizações nem mede FPS ou input lag**. Temperatura e utilização da GPU ainda não são coletadas. A leitura global de CPU acima de 64 processadores lógicos tem a limitação de grupos explicada no README.

O comportamento e os resultados no uso diário precisam ser avaliados no seu PC. O arquivo **Optimizer-windows-x64.zip.sha256** permite conferir a integridade do download.
