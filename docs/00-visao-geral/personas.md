# Personas

Usadas em: [cenários Gherkin](../01-requisitos/gherkin/) · [casos de uso](../01-requisitos/casos-de-uso/README.md)

Servem para simular os casos de uso e escrever os cenários. Todos os nomes são fictícios.

## Quem usa o simulador

### Ana — desenvolvedora da receptora
Integra o coletor. Quer ver o que acontece quando um banco devolve 429 no meio da paginação ou quando o consentimento cai entre duas páginas.

### Bruno — QA
Monta cenários repetíveis: "este cliente sempre dá timeout em saldos". Precisa que o resultado de hoje seja igual ao de amanhã.

### Carla — analista de produto
Conhece as regras do Open Finance e valida se o simulador se comporta como as instituições. Usa a jornada de consentimento para mostrar o fluxo ao negócio.

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
Reproduzem o volume e o comportamento dos bancos reais que mais pedem atenção. O nome leva o prefixo do ambiente: `HML-Itaú` e `HML-Nubank` em homologação, `DEV-Itaú` e `DEV-Nubank` em desenvolvimento ([RN09](../01-requisitos/regras-de-negocio.md#rn09)).

## Clientes sintéticos

### Mariana Alves — pessoa física
Cliente do Banco Estável, com uma conta, um cartão e um empréstimo. Usada na jornada PF.

### Rafael Lima — pessoa física com conta bloqueada
Tem uma conta disponível e outra com bloqueio temporário. Usado nos cenários de recursos.

### Indústria Aurora Ltda — pessoa jurídica
Cliente do Banco Estável, com duas aprovadoras: **Helena** e **Paula**. Usada na jornada PJ com múltiplas alçadas.

### Sócio único Comércio Sol — pessoa jurídica
Empresa com um único sócio pessoa natural, que aprova sozinho.
