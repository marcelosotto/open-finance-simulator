# Decisões da revisão de 05/11/2026

Revisão conduzida com Marcelo. Estas decisões já foram aplicadas na documentação. PA23 surgiu delas e segue em aberto.

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
| PA22 | Nascem prontos os 11 bancos de nome real da POC, com o perfil `estavel` e variáveis próprias, sem perfil novo. |

## Próximo passo

Revisão dos casos de uso, começando pelo UC01.
