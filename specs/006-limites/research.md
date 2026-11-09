# Research: Limites

## Soma atômica

- **Decision**: script Lua único: `INCR` e, no primeiro valor, `EXPIRE`.
- **Rationale**: chave nunca fica sem validade, e TPM e capacidade ficam exatos com várias instâncias (RN30).
- **Alternatives considered**: `INCR` seguido de `EXPIRE` em duas idas (pode deixar chave eterna); transação `MULTI` (expira sempre, estendendo a janela).

## Falha rápida

- **Decision**: `AbortOnConnectFail=false`, `BacklogPolicy.FailFast` e `AsyncTimeout` de 100 ms; qualquer `RedisException` ou tempo esgotado vira contagem zero e log.
- **Rationale**: Q8 e RNF03: sem conexão a chamada não pode esperar.
- **Alternatives considered**: fila padrão do cliente (segura a chamada durante a reconexão).

## Onde fica cada etapa

- **Decision**: `IEtapaDeEntrada` no `Despachante`, depois do portão; cota dentro de `ServicoDeDados`, logo após `PAGE_NOT_FOUND`.
- **Rationale**: Q4 pede um ponto único para as 75 operações; a cota depende do recurso, dos 422 e da chave.
- **Alternatives considered**: tudo no portão (quebraria RN39, pois 423 viria antes de 422).

## Validade das chaves

- **Decision**: capacidade 5 s; tráfego 120 s; cota 70 dias reais.
- **Rationale**: limpam sozinhas e, no caso da cota, sobrevivem ao mês mais longo, mesmo com relógio simulado.
- **Alternatives considered**: expirar no fim do mês (depende do relógio da instituição, RN73).

## Testes

- **Decision**: `IContadores` substituído nos testes. Por padrão, sem efeito; `ComContadoresEmMemoria()` nos cenários de limite; Redis real em teste de integração.
- **Rationale**: o relógio fixo e a massa pequena dos testes antigos estourariam capacidade e cota sem relação com o que eles provam.
- **Alternatives considered**: ajustar cada teste antigo.
