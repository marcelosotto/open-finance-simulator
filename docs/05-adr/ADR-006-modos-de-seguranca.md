# ADR-006: Modos de segurança simples e FAPI

**Status:** Proposto
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
