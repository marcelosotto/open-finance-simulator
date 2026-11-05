# Personas

Usadas em: [cenários Gherkin](../01-requisitos/gherkin/) · [casos de uso](../01-requisitos/casos-de-uso/README.md)

Servem para simular os casos de uso e escrever os cenários. Todos os nomes são fictícios.

## Quem usa o simulador

### Ana — desenvolvedora da receptora
Integra o coletor. Quer ver o que acontece quando um banco devolve 429 no meio da paginação ou quando o consentimento cai entre duas páginas.

### Bruno — QA
Monta cenários repetíveis: "este cliente sempre dá timeout em saldos". Precisa que o resultado de hoje seja igual ao de amanhã.

### Carla — analista de produto
Conhece as regras do Open Finance e valida se o simulador se comporta como as instituições. Cria consentimentos pelo painel para mostrar o fluxo ao negócio.

### Diego — operador de homologação
Cadastra bancos, gera lotes de massa, limpa o ambiente e acompanha o que foi configurado.

## Instituições simuladas

### Bancos de perfil
Têm nome fictício, que já diz o comportamento.

| Banco | Perfil | Como se comporta |
|---|---|---|
| Banco Estável | `estavel` | Dentro de todos os limites oficiais, poucos erros |
| Banco Lento | `lento` | Responde, mas com p95 acima do SLA oficial |
| Banco Instável | `instavel` | Muitos erros 5xx e janelas curtas de indisponibilidade |
| Banco Timeout | `timeout` | Parte das chamadas não termina dentro de 15 segundos |
| Banco Restritivo | `restritivo` | Aplica limites apertados e chave de paginação curta |

### Bancos com nome real
Usam o perfil `estavel`, sem perfil próprio. O que muda entre eles são as variáveis, como os consentimentos ativos (QCA), que definem o TPM da alta frequência. O valor da tabela vale para toda receptora e pode ser sobrescrito ([RN21](../01-requisitos/regras-de-negocio.md#rn21)). O nome leva o prefixo do ambiente: `HML-Itaú` com a marca `hml`, `DEV-Itaú` com `dev` e só `Itaú` com a marca vazia ([RN09](../01-requisitos/regras-de-negocio.md#rn09)).

Os consentimentos são valores fictícios, como ponto de partida, e não dados reais dos bancos.

| Banco | Nome em homologação | Consentimentos ativos |
|---|---|---|
| Itaú | `HML-Itaú` | 7.000.000 |
| Nubank | `HML-Nubank` | 6.500.000 |
| Bradesco | `HML-Bradesco` | 5.000.000 |
| Banco do Brasil | `HML-Banco do Brasil` | 4.500.000 |
| Caixa | `HML-Caixa` | 4.000.000 |
| Santander | `HML-Santander` | 3.000.000 |
| Inter | `HML-Inter` | 2.000.000 |
| C6 Bank | `HML-C6 Bank` | 1.500.000 |
| BTG Pactual | `HML-BTG Pactual` | 800.000 |
| Sicoob | `HML-Sicoob` | 700.000 |
| Sicredi | `HML-Sicredi` | 600.000 |

## Clientes simulados
São só massa de dados: não fazem login nem usam telas.

### Mariana Alves — pessoa física
Tem uma conta, um cartão e um empréstimo no Banco Estável.

### Rafael Lima — pessoa física com conta bloqueada
Tem uma conta disponível e outra com bloqueio temporário. Usado nos cenários de recursos.

### Indústria Aurora Ltda — pessoa jurídica
Tem produtos no Banco Estável e exige duas aprovações. Usada nos cenários de múltiplas alçadas.

### Sócio único Comércio Sol — pessoa jurídica
Empresa que não exige múltiplas alçadas.
