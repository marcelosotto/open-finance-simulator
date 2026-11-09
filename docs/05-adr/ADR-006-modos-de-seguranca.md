# ADR-006: Modos de segurança simples e FAPI

**Status:** Aceito
**Data:** 2026-11-05

## Contexto
Sem segurança, a receptora precisaria de cabeçalhos próprios, fora do caminho de produção. O perfil FAPI Brasil completo (mTLS, DCR, PAR, JARM, assinatura) é caro de construir e de operar em desenvolvimento.

## Decisão
Dois modos, por ambiente:
- **Simples:** token emitido por API própria, com receptora, consentimento e escopos. Sem mTLS. Serve para desenvolvimento.
- **FAPI:** o mais perto do perfil brasileiro, para homologação. Entra depois ([PA02](../01-requisitos/regras-de-negocio.md#pa02)).

Nos dois, a identidade da receptora, o cliente e o consentimento vêm do token, nunca de cabeçalho próprio.

## Consequências
O modo simples já tira os cabeçalhos próprios. O fornecedor do servidor de autorização do modo FAPI segue em aberto. O simulador não substitui a certificação oficial.

**Atualização da revisão:** o modo FAPI depende da jornada com redirecionamento, que foi [adiada](../01-requisitos/regras-de-negocio.md#adiados). Nesta versão, só o modo simples existe, e a receptora é cadastrada pelo operador.

**Atualização da spec 004:** o modo simples emite dois tokens por um único `POST /token`, no formato OAuth: o da receptora (`scope=consents`, equivalente ao `client_credentials` oficial) e o do consentimento (`scope=consent:{consentId}`). A renovação oficial pede `authorization_code`; aqui usa o token do consentimento, sem redirecionamento ([RN120](../01-requisitos/regras-de-negocio.md#rn120)).
