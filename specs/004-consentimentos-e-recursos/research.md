# Pesquisa: Consentimentos e recursos

## R01. Token assinado, sem gravação
- **Decisão**: token opaco para a receptora, com conteúdo assinado por HMAC-SHA256. A chave vem de `Simulador:Token:Chave`; sem configuração, é sorteada na subida.
- **Motivo**: evita leitura extra no caminho quente (RNF03) e gravação de cada token (X); o estado do consentimento é lido a cada chamada de qualquer forma. Em várias instâncias, a chave precisa estar configurada.
- **Alternativas**: coleção de tokens (mais escrita); JWT com biblioteca (dependência sem necessidade).

## R02. Módulo novo `Simulador.Consentimentos`
- **Decisão**: regras, token, receptoras e HTTP ficam no módulo novo. Consentimentos e recursos continuam gravados pela Massa, por uma porta nova `IEscritaDeMassa`. O host adapta as portas, sem referência entre módulos (ADR-007).
- **Motivo**: a Massa já é dona dessas coleções e dos ajustes.

## R03. Portão no Despachante
- **Decisão**: `Catalogo` ganha a porta `IPortaoDeOperacao`, chamada depois da situação do banco e antes do manipulador ou do 501. `OperacaoDoCatalogo` passa a trazer escopos e permissões lidos da spec.
- **Motivo**: etapa única do pipeline (ADR-008); rota sem `security` na spec segue sem portão.
- **Permissões**: vêm da seção "Permissions necessárias" da descrição de cada spec. Contratos sem essa seção (investimentos e câmbio) usam a família do contrato.

## R04. Vencimento calculado na leitura
- **Decisão**: consentimento com `ExpiraEm` vencido é tratado como `REJECTED` (`ASPSP`, `CONSENT_MAX_DATE_REACHED`) em toda leitura, sem job nem gravação.
- **Motivo**: repetível com relógio injetado (VI) e simples (X). Vale também para o mundo virtual.

## R05. Renovação de consentimento rejeitado
- **Decisão**: o portão não confere o estado do consentimento em `/extends`; a regra responde 422 (RN87, cenário "REJECTED é final"). Em Recursos e nas APIs de produto, o portão responde 401 (RN88).

## R06. Ajustes de consentimento e recurso virtual
- **Decisão**: `Ajuste` ganha razão, validade, atualização, extensões. Recurso virtual usa alvo `recurso:{id}` sem `RecursoId`, para não entrar nos ajustes de transação.

## R07. Grupos de permissões
- **Decisão**: tabela do contrato `consents-3.3.1` no domínio. Operações de Crédito e Investimento são um agrupamento único cada, como na tabela.

## R08. Paginação de `/resources`
- **Decisão**: `page` e `page-size` básicos (RN31, RN33), porque o contrato exige `links` e `meta`. A chave de paginação e o restante ficam na 005.
