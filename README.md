# Optimizer

Otimizador para PCs com Windows, em desenvolvimento, com foco na agilidade do sistema no uso diário. A etapa atual ajuda a investigar o consumo de recursos antes de implementar e medir ajustes. Melhorias em jogos serão um dos resultados a avaliar.

## Baixar e abrir, sem ferramentas de programação

1. Baixe [Optimizer para Windows x64 — versão de teste](https://github.com/jotazwy/Optimizer/releases/download/v0.2.0-preview/Optimizer-windows-x64.zip).
2. Clique no ZIP com o botão direito e escolha **Extrair tudo**.
3. Na pasta extraída, abra **Optimizer.App.exe**. Mantenha os demais arquivos junto dele.

O pacote é para Windows 10 atualizado/Windows 11 em PCs Intel/AMD de 64 bits e já inclui o .NET. A versão inicial somente faz diagnóstico; não executa limpezas nem altera a inicialização ou as configurações do Windows. Detalhes e verificações estão na [página da versão](https://github.com/jotazwy/Optimizer/releases/tag/v0.2.0-preview).

A prévia **0.2.0-preview** renova a apresentação para o diagnóstico geral do PC. A identidade combina preto profundo, painéis com aparência de vidro, reflexos sutis e acentos frios. A [captura da janela no Windows](https://github.com/jotazwy/Optimizer/releases/download/v0.2.0-preview/Optimizer-preview.png) acompanha os arquivos da versão.

## O que já existe

- Interface em português, feita com C# / .NET 10 e WPF, com identidade escura e painéis de vidro.
- Identificação do processador, placas de vídeo e sistema operacional.
- Uso de CPU e memória, atualizado automaticamente a cada 2 segundos.
- Até 8 processos ordenados por uso de CPU e, depois, memória.
- Recomendações quando CPU ou memória chegam a 85% de uso e indicação de um processo com consumo relevante de CPU.
- Diagnóstico local, sem exigir administrador, encerrar processos ou alterar configurações do PC.

Este painel ainda **não aplica otimizações**. Uma amostra de CPU/RAM não identifica sozinha a causa de lentidão. A identificação de GPU não inclui utilização, temperatura ou frequência.

## Executar a partir do código

1. Use Windows 10 atualizado ou Windows 11.
2. Instale o **.NET SDK 10.0.402**, ou patch compatível da série 10.0.4xx, pelo [site oficial](https://dotnet.microsoft.com/download/dotnet/10.0). O arquivo `global.json` registra a versão usada no projeto.
3. Abra um terminal na pasta `Optimizer`, que contém `Optimizer.sln`.
4. Execute:

```powershell
dotnet --version
dotnet restore Optimizer.sln --locked-mode
dotnet run --project src/Optimizer.App --configuration Release --no-restore
```

A janela abre e começa a coletar dados. Aguarde pelo menos duas leituras: CPU depende da diferença entre amostras. O botão **Atualizar agora** faz uma leitura adicional. Fechar a janela encerra o monitoramento.

Você também pode abrir `Optimizer.sln` em uma IDE com suporte a .NET 10 e WPF e definir `Optimizer.App` como projeto de inicialização.

## Como validar

```powershell
dotnet restore Optimizer.sln --locked-mode
dotnet build Optimizer.sln --configuration Release --no-restore
dotnet test tests/Optimizer.Core.Tests --configuration Release --no-build --no-restore
```

No **Windows**, execute também os testes que consultam os contadores reais do sistema:

```powershell
dotnet test tests/Optimizer.Windows.Tests --configuration Release --no-build --no-restore
```

No Linux, é possível compilar a interface usando `EnableWindowsTargeting` e executar os testes de lógica, mas **a janela WPF e os testes de integração exigem Windows**. Não execute `dotnet test` na solução inteira no Linux, pois ela inclui testes exclusivos de Windows.

Verificação manual no Windows:

1. Abra o aplicativo sem privilégios de administrador e confira os nomes de hardware.
2. Aguarde a segunda atualização e compare CPU/RAM com o Gerenciador de Tarefas durante alguns segundos; intervalos diferentes podem produzir valores diferentes.
3. Confira atualização automática, botão manual e ausência de travamento ao abrir/fechar outros programas.
4. Feche a janela e confirme que `Optimizer.App` terminou.

O fluxo de publicação executa os testes de lógica e de contadores reais no Windows, abre a janela e confere CPU/memória, aciona a atualização manual e verifica o encerramento. Ele também gera uma captura da janela para revisão visual. Isso complementa a verificação manual e não substitui a avaliação no seu PC.

## Organização

```text
src/Optimizer.App/             Interface WPF e coleta de métricas do Windows
src/Optimizer.Core/            Modelos, formatação e regras de diagnóstico
tests/Optimizer.Core.Tests/    Testes de lógica que também rodam no Linux
tests/Optimizer.Windows.Tests/ Testes dos contadores reais, exclusivos de Windows
```

A coleta usa APIs do Windows e leitura do Registro. Processos protegidos ou que terminem durante uma leitura podem ser omitidos; CPU indisponível aparece como `—`. A memória de cada processo é seu working set, que pode incluir páginas compartilhadas. O uso de CPU por processo é uma porcentagem da capacidade lógica total, não de um único núcleo. A identificação de placas pelo Registro pode incluir adaptadores instalados anteriormente.

Em máquinas com **mais de 64 processadores lógicos**, o contador global `GetSystemTimes` cobre o grupo do thread de coleta; esta versão não fornece uma leitura global confiável entre vários grupos. Esse caso precisa de uma implementação específica antes de interpretar a porcentagem como uso total.

## Próximos marcos

1. Validar a tela e as leituras em um PC Windows real.
2. Analisar armazenamento e apresentar uma prévia dos arquivos e do espaço recuperável, com seleção do usuário antes de implementar a limpeza.
3. Listar os aplicativos de inicialização em modo de leitura; depois, permitir alterações reversíveis com registro do estado anterior.
4. Criar histórico de diagnóstico para comparar antes e depois de cada ajuste em tarefas equivalentes.

As comparações devem observar tempo de inicialização, abertura de aplicativos, consumo de recursos e estabilidade. Medição de FPS e tempos de quadro poderá entrar como validação opcional para jogos. Ganhos de desempenho só devem ser anunciados depois de medidos; reduzir uso de memória, por si só, não comprova melhoria no uso do PC.
