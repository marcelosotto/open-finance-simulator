# Guia de uso do back-office

Relacionados: [comportamento de tela](../01-requisitos/interface.md) · [guia de front-end](README.md) · [regras de negócio](../01-requisitos/regras-de-negocio.md)

Este guia é para quem vai **usar** o simulador, sem precisar saber programar. O mesmo conteúdo aparece na página **Primeiros passos** do back-office ([UI18](../01-requisitos/interface.md#ui18)).

## O que é o simulador
Ele imita bancos do Open Finance, com clientes e dados de mentira. O seu app conversa com esses bancos como conversaria com os de verdade, e você vê o que acontece quando um banco demora, erra ou devolve dado ruim. Nada aqui é real: toda tela mostra a faixa "Ambiente simulado, dados fictícios".

## Como abrir
Com o simulador no ar (`docker compose up --build`), abra no navegador:

```
http://localhost:8081/
```

A primeira página é **Primeiros passos**. Toda tela tem, no topo, duas ou três linhas dizendo para que serve e o que precisa existir antes.

## Primeiros passos
Faça nesta ordem. A página marca como **Feito** o que já existe.

1. **Criar um banco** (tela *Bancos → Novo banco*). Já vêm 16 bancos prontos, como o Banco Estável e o Banco Lento. Crie outro só se quiser um comportamento próprio.
2. **Gerar clientes** (tela *Massa → Lotes*). Um lote cria muitos clientes de uma vez, com contas, cartões e transações. Para um cliente só, use *Massa → Clientes → Criar cliente avulso*.
3. **Criar um consentimento** (tela *Novo consentimento*). Escolha o banco e depois o cliente **na lista** desse banco; não é preciso saber o documento. Se o banco não tiver clientes, a tela avisa e leva para *Gerar massa de clientes*. Depois escolha as permissões e o prazo.
4. **Ver, aprovar ou revogar** (tela *Consentimentos*, ou o cliente em *Massa → Clientes*). Em clientes empresa, o consentimento pode pedir aprovações; os botões **Aprovar** e **Recusar** aparecem enquanto faltar alguma.
5. **Testar o app chamando a API**. O app pede um token ao banco com o usuário e a senha da receptora e chama as APIs Open Finance na porta 8080. Primeiros passos mostra um exemplo pronto, com as credenciais.

## A receptora padrão
A **receptora** é quem lê os dados: o seu app. O simulador já sobe com uma receptora pronta, chamada **XP** ([RN124](../01-requisitos/regras-de-negocio.md#rn124)), para você não precisar criar uma.

- O identificador dela é **fictício** (por exemplo, `hml-p-receptora-padrao`), nunca o de uma instituição real.
- Usuário e senha são de teste e aparecem na tela *Receptoras* e em Primeiros passos.
- Com uma só receptora, o menu de consentimento a usa direto. Com mais de uma, ele pergunta qual.
- Quem cuida da implantação pode trocar nome, identificador e credenciais pela configuração, sem mudar código:

| Configuração | Para quê | Padrão |
|---|---|---|
| `Simulador__ReceptoraPadrao__Nome` | Nome da receptora (vazio: sem receptora padrão) | `XP` |
| `Simulador__ReceptoraPadrao__OrganisationId` | Identificador fictício | `{marca}-p-receptora-padrao` |
| `Simulador__ReceptoraPadrao__Usuario` | Usuário de teste fixo | gerado na primeira subida |
| `Simulador__ReceptoraPadrao__Senha` | Senha de teste fixa | gerada na primeira subida |

Subir de novo não duplica a receptora nem troca as credenciais.

## Tela → para que serve

| Tela | Para que serve |
|---|---|
| Primeiros passos | O caminho, em ordem, com o que já foi feito marcado |
| Bancos | Os bancos simulados. O aviso amarelo mostra quando um banco tem um desvio de protocolo ligado de propósito |
| Novo banco | Cria um banco só com nome e perfil; o resto vem preenchido |
| Banco (ao abrir um) | Ajusta rapidez, erros, limites, desvios, relógio, e mostra o histórico |
| Novo consentimento | Simula um cliente autorizando outra instituição a ler os dados dele |
| Consentimentos | Abre um consentimento pelo identificador |
| Consentimento (ao abrir um) | Estado, permissões, recursos, aprovações; aprovar, recusar ou revogar |
| Receptoras | Quem lê os dados (o seu app), com usuário e senha de teste |
| Massa | Os clientes de mentira: perfis, distribuição, lotes, clientes e limpeza |
| Massa → Lotes | Gera muitos clientes de uma vez |
| Massa → Clientes | Abre um cliente escolhendo banco e cliente na lista, ou cria um avulso |
| Massa → Limpeza | Apaga massa; mostra antes o que será apagado |
| Cenários | Regras que mudam o comportamento de propósito (lentidão, erro, queda, dado ruim) |
| Cenários → Pacotes | Agrupa regras para ligar e desligar juntas |

## Quando algo dá errado
A tela diz o que faltou e como resolver. Exemplos:

- **"O Banco X ainda não tem clientes."** Gere um lote em *Massa → Lotes* ou crie um cliente avulso.
- **"Esse cliente não existe na massa deste banco."** Volte e escolha o cliente na lista.
- **"Nenhuma receptora cadastrada."** Crie uma em *Receptoras* (ou confira a configuração da receptora padrão).
- **Recusas com código**, como "Grupo de permissões incompleto (RN83)", vêm das regras do Open Finance: ajuste as permissões e tente de novo.
