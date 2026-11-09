# Decisões da revisão de 05/11/2026

Revisão conduzida com Marcelo. Estas decisões já foram aplicadas na documentação. Todas as perguntas foram fechadas.

## Premissas

| Premissa | Decisão |
|---|---|
| PR01 | Aprovada sem menção à POC. Novo texto: "Sem nenhuma configuração, uma instituição simulada segue as regras oficiais: limites, códigos HTTP, estados, paginação e formatos. Se a regra oficial existe, ela vale. A ordem das fontes resolve conflitos." A POC é só estudo e não entra na ordem das fontes. |
| PR02 | Aprovada. |
| PR03 | Aprovada (marcação em cabeçalho, prefixo de ID e campos de texto). |
| PR04 | Aprovada. |
| PR05 | Aprovada (semente opcional para sorteio de falhas e relógio fixável por banco). |
| PR06 | Aprovada. |
| PR07 | Aprovada. |
| PR08 | Ajustada: o foco é desenvolvimento e homologação, e o uso em produção não é bloqueado nem decidido agora. Se um dia houver uso em produção, decidir como fica a marcação dos dados. |

## Perguntas bloqueantes

| Pergunta | Decisão |
|---|---|
| PA01 | MongoDB para o estado, porque a empresa já usa. Redis para os contadores de limites, por desempenho. Confirma a ADR-003. |
| PA03 | Bancos inspirados em bancos reais usam o nome real com prefixo do ambiente: `HML-Itaú`, `HML-Nubank`; em desenvolvimento, `DEV-Itaú`. Os bancos de perfil (Banco Lento, Banco Timeout etc.) continuam com nome fictício. |
| PA04 | CPF com um dígito inicial fixo, permitindo até 100 milhões de CPFs. Risco aceito: não há garantia de que um CPF gerado não pertença a uma pessoa real. |
| PA05 | Não bloquear a subida em produção por enquanto (ver PR08). |
| PA02, PA06 a PA19, PA21 e PA23 | Aceitas as respostas sugeridas, registradas em `regras-de-negocio.md`. |
| PA22 | Nascem prontos os 11 bancos de nome real da POC, com o perfil `estavel` e variáveis próprias, sem perfil novo. |

## Nomenclatura

O projeto usa só **simulado** para o que é fictício: instituição, cliente e dado. A natureza do dado é **persistido** ou **virtual**. O termo "sintético" não é usado.

## Próximo passo

Revisão dos casos de uso. Blocos de configuração, massa de dados e consentimento aprovados.

## Front-end (2026-11-06)

Back-office em React com Webpack e Clean Architecture, sem login ([ADR-010](../05-adr/ADR-010-front-end-react.md) e [guia](../08-front-end/README.md)). O exemplo de padrão é o cadastro de bancos, e o commitlint vale só para `front/`.

## Mundo virtual

Existe um mundo virtual por ambiente, e um pedido se divide entre os bancos pela distribuição. O coletor pede o token normalmente, sem token na lista.

## Marca do ambiente

A marca do ambiente é um valor livre da configuração da implantação (`dev`, `hml`, `stage` ou outro). O sistema só concatena o valor, sem lista fechada nem regra, para saber de qual ambiente saiu a informação. Em produção a configuração fica vazia e nada é concatenado; a natureza (persistido ou virtual) continua aparecendo. Constituição emendada para a versão 2.0.0 ([RN60](../01-requisitos/regras-de-negocio.md#rn60), [RN70](../01-requisitos/regras-de-negocio.md#rn70)).

## Registro de chamadas

O simulador só responde. Quem registra e investiga as chamadas é a aplicação que consome, e por isso o UC26 foi removido.

## Consentimento sem jornada

Cliente simulado é só massa de dados, sem login nem telas. Consentimentos nascem `AUTHORISED`, pela API, pelo painel, por lote ou pelo mundo virtual. Intenção pendente, jornada com redirecionamento, consentimento recusado e modo FAPI ficaram [adiados](../01-requisitos/regras-de-negocio.md#adiados).

## Cliente e consentimento

O cliente não pertence a um banco. Ele pode ter produtos em vários bancos e mais de um consentimento no mesmo banco ([RN49](../01-requisitos/regras-de-negocio.md#rn49)).

## Limites

Todo banco nasce com os limites oficiais, e cada valor pode ser alterado depois, mesmo abaixo do oficial. Isso é configuração, não anomalia ([RN26](../01-requisitos/regras-de-negocio.md#rn26)). O QCA é um valor configurado ([RN21](../01-requisitos/regras-de-negocio.md#rn21)).
