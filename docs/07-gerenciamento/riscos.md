# Riscos

| ID | Risco | Efeito | Resposta |
|---|---|---|---|
| R01 | Perfis sem base em dados reais | Testes passam, e a produção surpreende | Perfis são estimativas; recalibrar se houver telemetria ([PA20](../01-requisitos/regras-de-negocio.md#pa20)) |
| R02 | Regras oficiais mudam | O simulador envelhece | Specs fixadas, tabela revisada, teste da tabela |
| R03 | Interpretação errada de uma regra | Time aprende um comportamento falso | Revisão humana antes de aprovar cada UC |
| R04 | Dado simulado confundido com real | Decisão errada em homologação | Marcação em três camadas |
| R05 | CPF gerado coincidir com pessoa real | Exposição indevida | Risco aceito: dígito fixo e marca nos nomes ([RN57](../01-requisitos/regras-de-negocio.md#rn57)) |
| R06 | Modo FAPI consumir o projeto | Atraso nas partes mais usadas | Spec própria, depois do modo simples |
| R07 | Simulador virar o gargalo do teste de carga | Mede o simulador, não o cliente | [RNF02](../01-requisitos/requisitos.md#rnf02) e [RNF03](../01-requisitos/requisitos.md#rnf03) |
| R08 | Simulador apontado por engano em produção | Dado falso em produção | Marca visível nos dados fora de produção; em produção a marca fica vazia por configuração, então a configuração da implantação é a proteção ([RN70](../01-requisitos/regras-de-negocio.md#rn70)) |
| R09 | Nome real de banco em dado simulado | Print ou log confundido com dado do banco de verdade | Prefixo do ambiente e marca nos dados ([RN09](../01-requisitos/regras-de-negocio.md#rn09)) |
