# Termo de Abertura do Projeto

## Problema
Simular dados de Open Finance é muito difícil. Os times que consomem dados de outras instituições não têm um ambiente onde possam colocar à prova as situações reais: um banco lento, um banco que dá timeout, um consentimento revogado no meio da coleta, uma paginação que expira, um dado fora do padrão.

Sem isso, o problema aparece só em produção, quando a instituição real se comporta de um jeito que ninguém testou.

## Objetivo
Oferecer um ambiente que se comporte **o mais perto possível do que as outras instituições financeiras oferecem de verdade**, nas APIs, na jornada de consentimento e nos defeitos, para uso em desenvolvimento e homologação.

## Stakeholders
| Papel | Descrição |
|---|---|
| Desenvolvedor da receptora | Integra o coletor e os produtos da XP contra o simulador |
| QA | Monta cenários e prova o comportamento do sistema em cada situação |
| Analista de produto | Entende o comportamento das instituições e valida as regras |
| Operador de homologação | Mantém o ambiente, os bancos e a massa de dados |
| Receptora (sistema) | Quem chama as APIs: o coletor da XP ou outro cliente |
| Revisores | Negócio, compliance e arquitetura, que aprovam as regras |

## Escopo inicial
- Cadastro de **instituições simuladas**, com bancos prontos: estável, lento, instável, timeout e restritivo, além de bancos com nome real e prefixo do ambiente, como `HML-Itaú`
- **Comportamento configurável**: latência, erros, indisponibilidade, timeout e cenários programados
- **Jornada de consentimento** o mais parecida possível com a real, com autenticação, confirmação e redirecionamento
- **API de Consentimentos e de Recursos**, com as máquinas de estados oficiais
- **APIs de dados cadastrais e transacionais** geradas a partir das specs oficiais
- **Limites oficiais**: tráfego (429), operacional (423) e capacidade (529)
- **Paginação oficial**, com `pagination-key`
- **Geração de massa**: dados persistidos e dados virtuais, com distribuição configurável
- **Defeitos de dados** injetados de propósito
- **Marcação** de todo dado com o ambiente e a natureza (persistido ou virtual)
- Back-office para operar tudo isso

## Fora de escopo (a definir)
- **Validar qualidade de dados.** O simulador **produz** dado ruim; quem valida é outro sistema ([PR04](premissas.md#pr04)).
- Iniciação de pagamentos e Pix, portabilidade e Open Insurance ([PA06](../01-requisitos/regras-de-negocio.md#pa06)).
- Dados abertos ([PA06](../01-requisitos/regras-de-negocio.md#pa06)).
- Decidir o uso em produção: não é o foco, mas também não é bloqueado ([RN70](../01-requisitos/regras-de-negocio.md#rn70)).
- Certificação: o simulador não prova conformidade com o Open Finance.

## Premissas e restrições
Ver [Premissas do projeto](premissas.md). As regras oficiais citadas foram lidas nas fontes preservadas em [referências](referencias.md), mas a interpretação ainda precisa de revisão humana.
