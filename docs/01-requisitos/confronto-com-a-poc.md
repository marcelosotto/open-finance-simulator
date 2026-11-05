# Confronto da POC com as regras oficiais

> A jornada com redirecionamento e a intenção pendente foram [adiadas](regras-de-negocio.md#adiados) depois desta análise.

Compara o `src/OpenFinance.FakeProxy` do repositório OpenToken (commit `d3fe839`) com as [fontes oficiais](../00-visao-geral/referencias.md). A comparação foi feita pela leitura do código, sem executar a POC. A interpretação das regras ainda precisa de revisão humana.

**Resumo:** a POC acerta a base (rotas a partir da spec, minuto cheio, faixas de TPM, chave de paginação de 60 minutos, consistência de saldo e semente). Ela erra na classe de 25 de 69 endpoints de dados, na contagem da cota, em vários códigos de retorno e não tem a máquina de estados do consentimento nem a segurança.

| Situação | O que significa |
|---|---|
| ✅ Conforme | Igual à regra oficial |
| ⚠️ Parcial | Existe, mas falta parte da regra |
| ❌ Diverge | Faz diferente da regra oficial |
| ➖ Ausente | Não existe na POC |

## Limites e minutos
| Tema | POC | Regra oficial | Situação | Regra |
|---|---|---|---|---|
| Janela do TPM | Conta por minuto cheio, com `epoch / 60` | Minuto cheio, de 00,000 a 59,999 | ✅ | [RN22](regras-de-negocio.md#rn22) |
| Faixas da alta frequência | Por consentimentos ativos, com as faixas oficiais | Igual | ✅ | [RN21](regras-de-negocio.md#rn21) |
| Chave do TPM | Transmissora × **classe** | Por **endpoint** e por origem | ❌ | [RN20](regras-de-negocio.md#rn20) |
| Valor padrão do TPM | Compose usa `TPM_SCALE=150` | Valores da tabela | ⚠️ Escala só para carga; não deve ser o padrão | [PR01](../00-visao-geral/premissas.md#pr01) |
| 429 | Só por TPM, mais um sorteio de 0,2% no perfil `Poc` | Exclusivo do TPM | ⚠️ O sorteio devolve 429 sem passar do limite | [RN23](regras-de-negocio.md#rn23) |
| Isenções | `consents` e `resources` | Token, DCR, Consents, Resources e Serviços | ⚠️ Faltam os grupos que a POC não implementa | [RN24](regras-de-negocio.md#rn24) |
| Contagem da cota | Conta antes do sorteio de erro, então 5xx e o próprio 423 entram | Só 2XX | ❌ | [RN27](regras-de-negocio.md#rn27) |
| Páginas seguintes | Não contam | Não contam | ✅ | [RN27](regras-de-negocio.md#rn27) |
| Dimensões da cota | Instituição, rota, objeto e cliente | Mês, endpoint, objeto, cliente e **receptora** | ⚠️ Só conhece uma receptora | [RN25](regras-de-negocio.md#rn25) |
| Valores da cota | 8, 30, 120, 240 e 420 | Igual | ✅ | [RN26](regras-de-negocio.md#rn26) |
| Cota abaixo do mínimo | `monthlyQuotaPercent` permite | Proibido | ⚠️ Útil, vira configuração com aviso na tela | [RN26](regras-de-negocio.md#rn26) |
| Capacidade (529) | Só erro sorteado; **não conta requisições por segundo** | 529 ao passar do limite de TPS | ➖ | [RN29](regras-de-negocio.md#rn29) |
| Contadores | Em memória, por instância | Únicos para o ambiente | ⚠️ Com duas instâncias, cada uma conta metade | [RN30](regras-de-negocio.md#rn30) |
| Mês da cota | Comprimido em 300 s no compose | Mês civil | ⚠️ Ferramenta de teste, não padrão | [RN73](regras-de-negocio.md#rn73) |

## <a id="classes-de-frequencia"></a>Classes de frequência
**25 de 69** endpoints de dados têm classe diferente da oficial. A própria POC avisa no arquivo que o mapa precisava ser revisado. Como a classe define TPM, cota e SLA, cada diferença muda 429 e 423.

| API | Endpoint | POC | Oficial |
|---|---|---|---|
| Contas | `/accounts/{accountId}/transactions` | Média | Baixa |
| Contas | `/accounts/{accountId}/reserved-balances` | Ausente | Alta |
| Cartão | `/accounts/{creditCardAccountId}` | Baixa | Média-alta |
| Cartão | `/accounts/{creditCardAccountId}/bills` | Baixa | Média |
| Cartão | `/accounts/{creditCardAccountId}/transactions` | Média | Baixa |
| Empréstimo | `/contracts` e `/contracts/{contractId}` | Baixa | Média |
| Empréstimo | `/contracts/{contractId}/payments` | Média | Média-alta |
| Financiamento, Adiantamento, Direitos Creditórios | `/contracts` | Baixa | Média |
| Renda Fixa Bancária e Crédito, Tesouro, Fundos | `/investments` | Baixa | Média |
| Renda Fixa Bancária e Crédito, Tesouro, Fundos | `/investments/{investmentId}/transactions` | Média | Baixa |
| Renda Variável | `/investments` | Baixa | Média |
| Renda Variável | `/investments/{investmentId}/balances` e `/transactions-current` | Média-alta | Média |
| Renda Variável | `/investments/{investmentId}/transactions` | Média | Baixa |
| Renda Variável | `/broker-notes/{brokerNoteId}` | Baixa | Média |
| Câmbio | `/operations` | Baixa | Média |

A [tabela de endpoints](tabela-de-endpoints.md) traz todos os valores oficiais.

## Paginação
| Tema | POC | Regra oficial | Situação | Regra |
|---|---|---|---|---|
| `page` e `page-size` | 1 e 25, máximo 1.000 | Igual | ✅ | [RN31](regras-de-negocio.md#rn31) |
| `page-size` mínimo | Aceita qualquer valor positivo | Mínimo de 25 nas APIs de dados | ❌ | [RN31](regras-de-negocio.md#rn31) |
| Página além do total | Devolve `data` vazio | 422 `PAGE_NOT_FOUND` | ❌ | [RN33](regras-de-negocio.md#rn33) |
| Máximo menor por instituição | Só global | A instituição pode ter o seu, com recálculo das páginas | ➖ | [RN32](regras-de-negocio.md#rn32) |
| `self` com tamanho aplicado | Repete o `page-size` pedido | Mostra o tamanho aplicado | ❌ | [RN32](regras-de-negocio.md#rn32) |
| Lista vazia | `totalPages` = 1 | `totalPages` = 0 | ❌ | [RN33](regras-de-negocio.md#rn33) |
| `first`, `prev`, `next`, `last` | Conforme a posição | Igual | ✅ | [RN34](regras-de-negocio.md#rn34) |
| Validade da chave | 3.600 s por banco | Até 60 minutos | ✅ | [RN35](regras-de-negocio.md#rn35) |
| Chave inválida ou expirada | Responde **422** | Nova chave, **sucesso** e conta na cota | ❌ | [RN36](regras-de-negocio.md#rn36) |

**A chave assinada com HMAC da POC é uma boa solução:** não exige guardar nada e amarra a chave aos filtros. O que muda é a reação quando ela falha.

## Padrões, datas e transações
| Tema | POC | Regra oficial | Situação | Regra |
|---|---|---|---|---|
| `x-fapi-interaction-id` ausente | Gera um e segue | Gera um e responde **400** | ❌ | [RN38](regras-de-negocio.md#rn38) |
| `x-fapi-interaction-id` válido | Devolve o mesmo | Igual | ✅ | [RN38](regras-de-negocio.md#rn38) |
| Janela de `/transactions-current` | Aceita até 12 meses | 7 dias no passado e 12 meses no futuro | ❌ | [RN41](regras-de-negocio.md#rn41) |
| Janela de `/transactions` | Até 12 meses entre `from` e `to` | 12 meses no passado e 12 no futuro, a partir de hoje | ⚠️ | [RN41](regras-de-negocio.md#rn41) |
| `from` sem `to` | Aceita | Os dois andam juntos | ❌ | [RN42](regras-de-negocio.md#rn42) |
| `/transactions-current` sem datas | 7 dias | Dia atual, pela spec | ❌ [PA09](regras-de-negocio.md#pa09) | [RN42](regras-de-negocio.md#rn42) |
| Estados da transação | Vêm do exemplo da spec | Futuro, processando e efetivada, com regra de ID | ➖ | [RN44](regras-de-negocio.md#rn44) |
| Contraparte por tipo | Não encontrada no gerador | Tabela oficial | ➖ | [RN45](regras-de-negocio.md#rn45) |
| Saldo e extrato | Coerentes | Sem regra específica | ✅ | [RN46](regras-de-negocio.md#rn46) |
| Versões das specs | Contas 2.4.2, Cartões 2.3.1, Cadastro 2.2.1, Empréstimos 2.5.0, Câmbio 1.0.0 | Contas 2.5.1, Cartões 2.4.0, Cadastro 2.3.0, Empréstimos 2.6.0, Câmbio 1.1.0 | ❌ Defasadas no corte de 25/10 | [PR06](../00-visao-geral/premissas.md#pr06) |

## Consentimento, jornada e recursos
| Tema | POC | Regra oficial | Situação | Regra |
|---|---|---|---|---|
| `POST /consents` | Devolve o exemplo da spec, sem criar nada | Cria em `AWAITING_AUTHORISATION` e valida permissões | ➖ | [RN80](regras-de-negocio.md#rn80) a [RN84](regras-de-negocio.md#rn84) |
| Criação de consentimento | API interna do sandbox, que já nasce autorizado | Passa pela jornada | ⚠️ Vale como atalho de teste | [UC07](casos-de-uso/README.md#uc07) |
| Expiração de 60 minutos | Ausente | `REJECTED` | ➖ | [adiado](regras-de-negocio.md#adiados) |
| `DELETE /consents/{id}` | Devolve o exemplo; a revogação usa API do sandbox | Revoga | ➖ | [RN86](regras-de-negocio.md#rn86) |
| Renovação | Devolve o exemplo | Regras e erros próprios | ➖ | [RN87](regras-de-negocio.md#rn87) |
| Consentimento revogado | Responde **403** `CONSENT_STATUS_REJECTED` | Responde **401** | ❌ | [RN88](regras-de-negocio.md#rn88) |
| Jornada do cliente | Tela do sandbox, sem login, confirmação ou redirecionamento | Seis etapas | ➖ | [adiado](regras-de-negocio.md#adiados) |
| 36 permissões oficiais | Sim | Sim | ✅ | [RN83](regras-de-negocio.md#rn83) |
| Estados do recurso | Os quatro | Os quatro | ✅ | [RN100](regras-de-negocio.md#rn100) |
| Transições do recurso | Edição livre | Transições restritas | ⚠️ | [RN101](regras-de-negocio.md#rn101) |
| 202 de Recursos | Ausente | 202 por até 5 minutos, depois 504 | ➖ | [RN103](regras-de-negocio.md#rn103) |
| 403 com `STATUS_RESOURCE_*` | Sim | Sim | ✅ | [RN104](regras-de-negocio.md#rn104) |
| Recurso indisponível na listagem | Não encontrei o filtro | Não aparece na lista de produto | ⚠️ A conferir | [RN104](regras-de-negocio.md#rn104) |
| Múltiplas alçadas | Só o estado | Fluxo completo, por aprovador | ➖ | [RN98](regras-de-negocio.md#rn98) |

## Comportamento, segurança e operação
| Tema | POC | Proposta | Situação | Regra |
|---|---|---|---|---|
| Latência | Base + variação + cauda | Por classe ou endpoint, com referência no SLA | ⚠️ | [RN10](regras-de-negocio.md#rn10) |
| Timeout | O 504 só soma a cauda lenta (1.500 ms) | Segura até 15 s e termina como configurado | ➖ | [RN12](regras-de-negocio.md#rn12) |
| Banco lento e banco timeout | Não existem | Perfis prontos | ➖ | [RN02](regras-de-negocio.md#rn02) |
| Falhas de transporte | Ausentes | Conexão cortada, corpo truncado, JSON inválido | ➖ | [RN17](regras-de-negocio.md#rn17) |
| Cenários por cliente ou período | Ausentes | Regras de cenário | ➖ | [RN14](regras-de-negocio.md#rn14) |
| Defeitos de dados | Ausentes, o MQD fica perto de 100% | Catálogo de defeitos | ➖ | [RN110](regras-de-negocio.md#rn110) |
| Instituição chamada | Cabeçalho `x-of-institution` | Endereço da instituição | ❌ Exige mudar o cliente | [RN07](regras-de-negocio.md#rn07) |
| Consentimento e cliente na chamada | Cabeçalhos `x-of-consent-id` e `x-of-client-id` | Vêm do token | ❌ | [RN122](regras-de-negocio.md#rn122) |
| Segurança | Nenhuma: sem token, mTLS ou DCR | Modos simples e FAPI | ➖ | [RN120](regras-de-negocio.md#rn120) |
| APIs de administração | Abertas, sem usuário | Separadas, com usuário e histórico | ➖ | [RN71](regras-de-negocio.md#rn71) |
| Marcação | Só o prefixo `v` em IDs virtuais | Ambiente e natureza em três lugares | ⚠️ | [RN60](regras-de-negocio.md#rn60) |
| Sorteio | `Random.Shared`, não repete | Semente fixa quando o cenário pedir | ⚠️ | [RN54](regras-de-negocio.md#rn54) |

## Semente e nomenclatura

**Nomenclatura.** Tudo no projeto é **simulado**: instituição, cliente e dado. Um dado simulado tem uma de duas naturezas: **persistido**, gravado no banco (o "físico"), ou **virtual**, calculado na hora ([RN50](regras-de-negocio.md#rn50)).

**A semente é válida e deve ficar.** Ela resolve duas coisas difíceis: transações não ocupam banco, e o mesmo dado volta sempre, mesmo em milhões de clientes. O índice carregado no ID do mundo virtual também é uma boa ideia.

O que muda:
- **Bancos:** o sorteio por `frac(n × φ)` continua, mas lendo a distribuição do banco de dados.
- **Prefixo:** hoje todo ID iniciado por `v` é tratado como virtual. O prefixo novo inclui o ambiente ([RN62](regras-de-negocio.md#rn62)).
- **Estado sobre o virtual:** a POC não revoga um consentimento virtual. Com **ajustes**, passa a poder ([RN53](regras-de-negocio.md#rn53)).
- **Gerador:** hoje os registros partem do exemplo da spec. Ele precisa conhecer estados de transação e contraparte ([RN44](regras-de-negocio.md#rn44) e [RN45](regras-de-negocio.md#rn45)).

## Onde guardar os dados

| O que | POC | Problema | Proposta |
|---|---|---|---|
| Bancos e configuração | `simulation-banks.json` em volume | Sem histórico nem concorrência; é o arquivo que você citou | MongoDB |
| Clientes, consentimentos, recursos | ClickHouse com `ReplacingMergeTree` e `FINAL` | Banco analítico para estado transacional: cada edição é uma linha nova, e a máquina de estados do consentimento precisa de atualização atômica | MongoDB |
| Transações | Não gravadas | Nenhum | Continua virtual, com ajustes no MongoDB |
| Contadores | Memória | Não funciona com várias instâncias | Redis |

A decisão e as alternativas, inclusive PostgreSQL, estão na [ADR-003](../05-adr/ADR-003-mongodb-e-redis.md).

## O que aproveitar da POC
- Rotas geradas das specs
- Chave de paginação assinada
- Gerador por semente e por dia, e o mundo virtual
- Consistência de saldo, limite e fatura
- Perfis de dados e geração em lote
- CPF válido e validação de shares
- 37 testes, como ponto de partida para os cenários
