# Research: Consentimento no painel

## R01. Onde o estado no consentimento é aplicado
- **Decision**: função pública `ExtensoesDosConsentimentos.EstadoNoConsentimento(consentimento, estadoGravado, agora)`. O host a aplica no estado gravado e só depois chama a regra de cenário (`IComportamentoDaChamada.EstadoDoRecurso`). Em Recursos, por um método novo de `IAmbienteDosConsentimentos`; nas APIs de produto, no adaptador de Dados do host, com o consentimento que o portão guardou na chamada.
- **Rationale**: RN97 põe a regra de cenário por cima; Dados já pede o recurso ao host, e o consentimento validado já está na chamada. O módulo Dados, alterado pela 009, não muda.
- **Alternatives**: mudar a assinatura de `IAmbienteDosDados.Recurso`; conflita com a 009 sem ganho.

## R02. Onde a alçada é guardada
- **Decision**: `AprovacoesPendentes` (já existe), mais `AlcadaRecusada` e `Decisoes` opcionais no consentimento. O prazo é calculado: criação + 15 dias.
- **Rationale**: o consentimento é gravado como JSON; campo opcional não quebra o que já existe.

## R03. Concorrência nas decisões
- **Decision**: releitura e gravação por versão, como na revogação (até 5 tentativas). Cada tentativa reavalia a situação, então a segunda aprovação da última pendência recebe 409.

## R04. Relógio
- **Decision**: criação, consulta e decisões usam `AgoraNoBanco` (administração fora da chamada do banco); Recursos e APIs de produto usam o instante da chamada (`TimeProvider`), que já é o do banco.

## R05. Grupos do menu
- **Decision**: os 13 grupos oficiais de `Permissoes` ganham nome estável (ex.: `contas-saldos`) e classificação PF, PJ ou ambos. A criação acrescenta `RESOURCES_READ` ao pedido.
