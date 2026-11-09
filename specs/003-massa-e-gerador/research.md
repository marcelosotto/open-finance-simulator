# Pesquisa: Massa e gerador

## R01. Sorteio determinístico
**Decisão:** SplitMix64 próprio, com a chave derivada de semente e rótulos por SHA-256.
**Motivo:** `System.Random` não garante a mesma sequência entre versões do .NET, e uma função pura por índice permite calcular qualquer item sem os anteriores (RN52, RN54).
**Alternativas:** `System.Random` e `System.IO.Hashing` (dependência nova sem necessidade).

## R02. Atributos e identidade em fluxos separados
**Decisão:** atributos dependem de `semente + índice`. Documento e IDs dependem de uma **posição** reservada em um contador do ambiente.
**Motivo:** dois lotes com a mesma semente recebem posições diferentes, logo documentos e IDs mudam e o resto fica igual; em ambiente limpo, a posição começa em 0 e tudo se repete (RN54).
**Alternativas:** hash de semente e número da ocorrência (pode colidir entre sementes).

## R03. Espaço de documentos
**Decisão:** CPF é `9` + 8 dígitos sequenciais. O mundo virtual usa os blocos de 0 a 49.999.999, e a massa persistida, de 50.000.000 a 99.999.998. CNPJ usa raiz alfanumérica com `V` ou `P` e a posição em base 36, com a filial `0001`.
**Motivo:** garante FR-007 sem consulta e sem colisão. O CPF `99999999999` é excluído por ter todos os dígitos iguais. O padrão oficial aceita CNPJ alfanumérico (`^[0-9A-Z]{12}[0-9]{2}$`).
**Consequência:** mundo virtual até 50 milhões, e reservas além do saldo são recusadas (RN57).

## R04. Banco de cada consentimento
**Decisão:** ciclo de 10.000 posições, embaralhado pela semente, com a quantidade de cada banco proporcional à porcentagem (até 2 casas decimais). A sobra vai para os maiores restos.
**Motivo:** é o sorteio de RN56 e permite achar o n-ésimo item de um banco em O(1), sem varrer o mundo.
**Alternativas:** sorteio por item, que obriga a varrer ao filtrar por banco.

## R05. Item do mundo virtual
**Decisão:** um item é um cliente com um consentimento `AUTHORISED` e seus recursos. IDs carregam o índice: `{marca}-v-r{base36}x{n}` e `urn:{marca}:v-c{base36}x0`.
**Motivo:** resolver o item pelo ID sem gravar nada (RN52, FR-019).

## R06. Transações
**Decisão:** cada recurso guarda uma semente de dados. Transações saem de `semente + dia`; o saldo soma os dias desde o início do histórico mais os ajustes.
**Motivo:** ADR-004 e RN46. Cartões e faturas usam as mesmas transações na spec 005, que define o formato oficial deles.

## R07. Lote em segundo plano
**Decisão:** fila em memória consumida por um serviço hospedado, gravando blocos de 500 clientes e atualizando o progresso a cada bloco. O cancelamento é um campo no lote, lido a cada bloco. Ao subir, lote sem atualização há 2 minutos vira `parcial`, com motivo `interrompido`.
**Motivo:** a solução mais simples que funciona com várias instâncias (RN30); não retoma sozinho (edge case da spec).

## R08. Gravação
**Decisão:** documentos em JSON no campo `doc`, como na spec 002, com os campos consultados (`loteId`, `banco`, `cliente`) ao lado.
**Motivo:** mantém um único formato no projeto.

## R09. Histórico
**Decisão:** usar `IRepositorioDeInstituicoes.AcrescentarHistorico` pelo Host, com a chave `massa:geral`, e um registro por operação.
**Motivo:** RN71 e FR-024, sem nova coleção.

## R10. Limpeza total com mundo virtual definido
**Decisão:** instituições e perfis criados pelo operador que o mundo virtual usa ficam.
**Motivo:** a definição do mundo fica (UC27) e não pode apontar para algo apagado.

## R11. Permissões do lote
**Decisão:** a 003 confere se o nome existe nas specs travadas e remove as do outro tipo de pessoa. A validação de grupos (RN83) fica na 004.
**Motivo:** o padrão da distribuição já é válido, e RN83 pertence à API de consentimento.
