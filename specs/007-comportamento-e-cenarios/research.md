# Research: Comportamento e cenários

## R01. Como o relógio do banco chega aos módulos
- **Decision**: `RelogioDaChamada : TimeProvider`, registrado como o `TimeProvider` da aplicação. Um filtro no grupo das rotas oficiais lê a instituição e guarda o instante simulado na chamada; `GetUtcNow` devolve esse instante. Fora de chamada com relógio alterado, devolve a hora real.
- **Rationale**: dezenas de leituras em seis módulos passam a usar o relógio do banco sem mudança, e o instante é único por chamada.
- **Alternatives**: passar o relógio por parâmetro a cada serviço; muito código e fácil de esquecer um ponto.

## R02. Hora real
- **Decision**: `HoraReal(TimeProvider)` em `Comum`, usada por TPM, capacidade, espera e cálculo do relógio simulado.
- **Rationale**: RN73 explícita; a fábrica de testes troca só `HoraReal`.

## R03. Ponto da etapa
- **Decision**: `Despachante` guarda a operação na chamada. Dados chama a etapa depois da cota; cada operação de Consentimentos, depois das validações e antes de gravar.
- **Rationale**: RN39 e FR-017. Acesso, parâmetros e cota estão dentro dos manipuladores.
- **Alternatives**: etapa no `Despachante`; rodaria antes de 403, 422 e 423.

## R04. Falhas de transporte
- **Decision**: conexão cortada aborta na etapa. Corpo truncado e JSON inválido marcam a chamada, e o filtro corrompe a saída do resultado.
- **Rationale**: o 200 sai e a cota conta (Q13).

## R05. Espera
- **Decision**: `IEsperaDaChamada`; a real usa `Task.Delay` em `HoraReal`, e os testes registram a duração sem esperar.
- **Rationale**: princípio IX. Espera no relógio falso nunca terminaria sem avanço.

## R06. Suítes anteriores
- **Decision**: a fábrica de testes troca a etapa por uma nula; só quem pede `ComComportamento()` tem latência e sorteio.
- **Rationale**: sem flag de produção.

## R07. Regras de estado
- **Decision**: disputam entre si, à parte das demais, porque agem na etapa de acesso. Exigem frequência 100%: a sobreposição precisa valer igual para lista e acesso direto.
- **Rationale**: Q2, ressalva de RN39.

## R08. Cache
- **Decision**: lista de regras ativas em memória por 5 s (RNF12), invalidada por escrita na mesma instância.

## R09. Latência
- **Decision**: curva por partes: 0–50% vai de metade do típico ao típico; 50–95% do típico ao típico + variação; 95–100% até típico + 2 × variação.
- **Rationale**: dá p50 e p95 configurados, sem biblioteca.
