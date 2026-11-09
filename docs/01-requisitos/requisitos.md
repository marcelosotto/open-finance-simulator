# Requisitos

Documentos relacionados: [Regras de negócio](regras-de-negocio.md) · [Casos de uso](casos-de-uso/README.md) · [Cenários Gherkin](gherkin/) · [Glossário](glossario.md)

## Funcionais
| ID | Descrição | Caso de uso |
|---|---|---|
| <a id="rf01"></a>RF01 | Operador cadastra, edita, inativa e reativa instituições simuladas | [UC01](casos-de-uso/README.md#uc01) |
| <a id="rf02"></a>RF02 | O ambiente nasce com os bancos estável, lento, instável, timeout e restritivo | [UC01](casos-de-uso/README.md#uc01) |
| <a id="rf03"></a>RF03 | Operador configura latência, erros, limites e paginação de cada instituição | [UC02](casos-de-uso/README.md#uc02), [UC03](casos-de-uso/README.md#uc03) |
| <a id="rf04"></a>RF04 | QA programa regras de cenário por instituição, endpoint, cliente ou recurso | [UC04](casos-de-uso/README.md#uc04) |
| <a id="rf05"></a>RF05 | Operador define perfis de dados e a distribuição da massa | [UC05](casos-de-uso/README.md#uc05) |
| <a id="rf06"></a>RF06 | Usuário cria um cliente simulado ou um lote, com consentimentos e recursos | [UC06](casos-de-uso/README.md#uc06), [UC07](casos-de-uso/README.md#uc07) |
| <a id="rf07"></a>RF07 | Teste de carga usa o mundo virtual, sem gravar dados | [UC08](casos-de-uso/README.md#uc08) |
| <a id="rf08"></a>RF08 | Receptora cria, consulta, revoga e renova consentimentos pelas APIs oficiais | [UC09](casos-de-uso/README.md#uc09), [UC12](casos-de-uso/README.md#uc12), [UC13](casos-de-uso/README.md#uc13) |
| <a id="rf09"></a>RF09 | Usuário cria consentimentos PF e PJ pelo painel, já autorizados | [UC10](casos-de-uso/README.md#uc10), [UC11](casos-de-uso/README.md#uc11) |
| <a id="rf10"></a>RF10 | Receptora consulta recursos, com estados, preparação (202) e seleção | [UC14](casos-de-uso/README.md#uc14) |
| <a id="rf11"></a>RF11 | Operador muda o estado de recursos e adiciona produtos | [UC15](casos-de-uso/README.md#uc15) |
| <a id="rf12"></a>RF12 | Receptora consulta dados cadastrais e transacionais pelas specs oficiais | [UC16](casos-de-uso/README.md#uc16) |
| <a id="rf13"></a>RF13 | Listas seguem a paginação oficial | [UC17](casos-de-uso/README.md#uc17) |
| <a id="rf14"></a>RF14 | O simulador aplica TPM, limite operacional e capacidade | [UC18](casos-de-uso/README.md#uc18), [UC19](casos-de-uso/README.md#uc19), [UC20](casos-de-uso/README.md#uc20) |
| <a id="rf15"></a>RF15 | O simulador reproduz timeout, indisponibilidade e falha de transporte | [UC21](casos-de-uso/README.md#uc21) |
| <a id="rf16"></a>RF16 | QA injeta defeitos de dados e anomalias de protocolo | [UC22](casos-de-uso/README.md#uc22) |
| <a id="rf17"></a>RF17 | Todo dado gerado traz a marca de ambiente e natureza | [UC23](casos-de-uso/README.md#uc23) |
| <a id="rf18"></a>RF18 | Desenvolvedor atualiza a versão das specs oficiais | [UC24](casos-de-uso/README.md#uc24) |
| <a id="rf19"></a>RF19 | Receptora se autentica no modo simples | [UC25](casos-de-uso/README.md#uc25) |
| <a id="rf21"></a>RF21 | Operador limpa massa e apaga lotes | [UC27](casos-de-uso/README.md#uc27) |
| <a id="rf22"></a>RF22 | QA fixa ou acelera o relógio de uma instituição | [UC28](casos-de-uso/README.md#uc28) |

## Não funcionais
| ID | Descrição |
|---|---|
| <a id="rnf01"></a>RNF01 | **Conformidade de contrato:** com perfil `estavel` e sem defeitos, toda resposta valida contra o schema oficial da versão. |
| <a id="rnf02"></a>RNF02 | **Capacidade:** cada instância atende pelo menos 300 requisições por segundo, para que o limite simulado venha da regra e não do próprio simulador. A meta final depende do volume de homologação ([PA16](regras-de-negocio.md#pa16)). |
| <a id="rnf03"></a>RNF03 | **Latência própria:** o simulador acrescenta no máximo 10 ms no p95 além da latência configurada. |
| <a id="rnf04"></a>RNF04 | **Elasticidade:** várias instâncias compartilham contadores e configuração ([RN30](regras-de-negocio.md#rn30)). |
| <a id="rnf05"></a>RNF05 | **Repetibilidade:** o mesmo cenário com a mesma semente produz o mesmo resultado ([PR05](../00-visao-geral/premissas.md#pr05)). |
| <a id="rnf06"></a>RNF06 | **Volume:** o mundo virtual suporta pelo menos 10 milhões de itens sem gravar dados. |
| <a id="rnf07"></a>RNF07 | **Atualização de specs:** trocar o commit das specs não exige mudar regra de negócio, salvo quando a versão nova traz regra nova. |
| <a id="rnf08"></a>RNF08 | **Testes:** cobertura de 100% no domínio e 80% no total, com os cenários Gherkin rodando no CI. |
| <a id="rnf09"></a>RNF09 | **Observabilidade:** logs, métricas e traces em OpenTelemetry. |
| <a id="rnf10"></a>RNF10 | **Sem dado real:** nenhum componente aceita ou guarda dado real ([PR03](../00-visao-geral/premissas.md#pr03)). |
| <a id="rnf11"></a>RNF11 | **Execução local:** o ambiente completo sobe com Docker Compose. |
| <a id="rnf12"></a>RNF12 | **Configuração sem deploy:** mudanças de comportamento valem sem reiniciar ([RN05](regras-de-negocio.md#rn05)), em até 5 segundos. |
