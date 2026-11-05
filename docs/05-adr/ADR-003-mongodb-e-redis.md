# ADR-003: MongoDB para estado e Redis para contadores

**Status:** Aceito
**Data:** 2026-11-05

## Contexto
A POC guarda bancos em arquivo JSON, massa no ClickHouse e contadores em memória. O arquivo não tem histórico nem concorrência. O ClickHouse é analítico: cada edição vira linha nova, lida com `FINAL`, e a máquina de estados do consentimento precisa de atualização atômica. Contador em memória não funciona com várias instâncias.

## Decisão
- **MongoDB** para bancos, clientes, consentimentos, recursos, ajustes, cenários e histórico.
- **Redis** para TPM, TPS e limite operacional, por desempenho.
- **Sem ClickHouse** no simulador.

**Por que MongoDB:** a empresa já usa, então não é preciso implantar um banco novo. Além disso, os documentos têm a forma das respostas da API, e configurações de banco variam bastante. Atualização atômica cobre os estados, e índice TTL cobre as expirações.

## Alternativas consideradas
- **PostgreSQL:** ótimo para os estados e para integridade entre tabelas, mas seria um banco novo para implantar.
- **Só MongoDB:** contadores por chamada, em todo o tráfego, pesariam no banco.
- **Manter ClickHouse:** bom para análise, inadequado para estado.

## Consequências
Duas dependências no ambiente, as mesmas que a POC já usava no coletor. Redis perdido zera contadores, o que é aceitável em homologação.
