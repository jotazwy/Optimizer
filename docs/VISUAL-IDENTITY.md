# Identidade visual do Optimizer

O Optimizer é um aplicativo de desempenho geral do PC. A linguagem visual combina preto profundo, superfícies de vidro e iluminação fria discreta. A interface deve dar destaque aos dados reais e às consequências de cada ação.

## Elementos

| Elemento | Direção |
| --- | --- |
| Base | Preto `#080A0E`, com iluminação ambiente suave |
| Superfícies | Gradientes translúcidos, contorno fino e cantos de 16–18 px |
| Texto principal | Branco frio `#EDF1F7` |
| Texto secundário | Cinza `#929EAE` |
| Destaque | Ciano `#75DCEB` para seleção e leituras de CPU |
| Destaque secundário | Lavanda `#ACA8FF` para memória |
| Atenção | Âmbar `#EAB978`, reservado para situações que merecem investigação |
| Tipografia | Segoe UI; peso semibold nos títulos, números grandes para métricas |
| Marca | Anel hexagonal em gradiente frio; fonte vetorial em `src/Optimizer.App/Assets/Optimizer.svg` |

## Aplicação

- Reutilize os recursos e estilos de `App.xaml` nas próximas telas.
- Use reflexos, bordas e gradientes para sugerir profundidade. Evite desfocar a janela inteira ou animar efeitos continuamente.
- Preserve contraste, foco de teclado e estados de interação claros.
- Gráficos devem representar leituras reais. Enquanto faltarem amostras, mantenha a área sem linha.
- Mostre estado de coleta, atualização e falha; uma interface bonita não deve sugerir que dados antigos estão atuais.
- A navegação aponta para seções existentes. Novos módulos só devem aparecer como ações quando estiverem implementados.
- O acabamento não exige transparência sobre a área de trabalho. As camadas de vidro são renderizadas dentro do aplicativo, com acabamento nativo de janela quando disponível.

O ícone `.ico` acompanha o projeto para que builds e pacotes usem a mesma marca, sem exigir ferramentas gráficas na instalação.
