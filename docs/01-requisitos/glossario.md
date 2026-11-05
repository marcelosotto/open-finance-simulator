# Glossário

Relacionados: [regras de negócio](regras-de-negocio.md) · [modelo de domínio](../03-design/classes/modelo-de-dominio.md)

| Termo | Definição |
|---|---|
| **Simulador** | Este sistema. Substitui o nome FakeProxy da POC. |
| **Instituição simulada** | Banco fictício que responde às APIs como uma transmissora. Na tela, "banco". |
| **Transmissora** | Instituição que entrega os dados do cliente. No simulador, sempre uma instituição simulada. |
| **Receptora** | Instituição que consome os dados. Ex.: o coletor da XP. |
| **Perfil de comportamento** | Modelo de como uma instituição responde: latência, erros, limites e anomalias. Ex.: `lento`. |
| **Perfil de dados** | Modelo de como os dados de um cliente se parecem: volume, valores e produtos. Ex.: `alto-volume`. |
| **Dado sintético** | Qualquer dado criado artificialmente. Todo dado do simulador é sintético. |
| **Dado persistido** | Dado sintético gravado no banco. É o "dado físico". |
| **Dado virtual** | Dado sintético calculado na hora a partir de uma semente, sem gravar. |
| **Mundo virtual** | Conjunto de clientes, consentimentos e recursos virtuais, usado em teste de carga. |
| **Massa** | Conjunto de dados sintéticos criado para um teste. |
| **Lote** | Massa criada de uma vez, que pode ser apagada de uma vez. |
| **Semente** | Valor que faz o gerador produzir sempre o mesmo resultado. |
| **Ajuste** | Alteração persistida sobre um dado calculado. Ex.: mudar uma transação ou revogar um consentimento virtual. |
| **Distribuição** | Como a massa se divide entre instituições, produtos e perfis. |
| **Marca** | Indicação de ambiente e natureza presente em todo dado. |
| **Regra de cenário** | Mudança de comportamento com alcance e período. Ex.: "este cliente sempre dá timeout". |
| **Defeito de dado** | Alteração proposital do conteúdo da resposta. |
| **Anomalia de protocolo** | Desvio proposital de uma regra oficial. |
| **Registro de chamada** | Anotação de cada chamada recebida, com a regra aplicada. |
| **TPM** | Transações por minuto: limite de tráfego por endpoint e origem. Resposta 429. |
| **TPS** | Transações por segundo: capacidade da instituição. Resposta 529. |
| **Limite operacional** | Teto mensal por endpoint, objeto, cliente e receptora. Resposta 423. |
| **QCA** | Quantidade de consentimentos ativos entre receptora e transmissora. Define o TPM da alta frequência. |
| **Classe de frequência** | Alta, média-alta, média ou baixa. Define TPM, limite operacional e SLA. |
| **`pagination-key`** | Identificador criado pela transmissora para não contar as páginas seguintes na cota. |
| **Hybrid flow** | Fluxo com redirecionamento para a instituição autenticar e confirmar. |
| **Alçada** | Aprovação de uma pessoa autorizada. Uma empresa pode exigir mais de uma. |
| **Seleção de origem** | Escolha de quais contas ou cartões compartilhar. |
