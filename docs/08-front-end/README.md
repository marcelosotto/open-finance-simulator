# Guia de front-end

Relacionados: [ADR-010](../05-adr/ADR-010-front-end-react.md) · [comportamento de tela](../01-requisitos/interface.md) · [estratégia de testes](../06-testes/estrategia-de-testes.md)

Regras do back-office do simulador. Valem para todo código em `front/`. As telas e o que elas fazem estão em [comportamento de tela](../01-requisitos/interface.md); aqui fica só **como** construir.

## Para que serve o front
- Rodar e testar o simulador e ver as telas: bancos, massa, consentimentos, cenários e limites.
- **Sem login e sem autenticação.** Quem abre a tela já pode usar ([RN71](../01-requisitos/regras-de-negocio.md#rn71)).
- Fala só com a API de administração. Nunca chama as APIs Open Finance para configurar nada.
- Mostra o ambiente em destaque em todas as telas ([UI01](../01-requisitos/interface.md#ui01)).

## Regras absolutas
1. Usar **apenas** as bibliotecas open source da [stack](#stack). Biblioteca nova exige ADR.
2. **Webpack 5** como bundler, com configurações separadas: `webpack.common.ts`, `webpack.dev.ts` e `webpack.prod.ts`, unidas por `webpack-merge`.
3. **Jest 30** como test runner, com `ts-jest` ^29.4, a linha que suporta o Jest 30 (não existe `ts-jest` 30).
4. **Clean Architecture** rigorosa, com dependência só para dentro: `core` → `application` → `infrastructure` → `presentation`. Detalhes em [camadas](#camadas).
5. **Either Pattern** no retorno de todo caso de uso. Detalhes em [Either](#either).
6. **Injeção de dependência** com `tsyringe` para desacoplar as camadas. O Babel MUST ter suporte a decorators, e `reflect-metadata` é importado uma vez, no ponto de entrada.
7. **ESLint, Prettier, Husky, lint-staged e commitlint** configurados, valendo só para a pasta `front/`.
8. **Testes** de entidades, casos de uso e componentes. Cobertura mínima: **100% no `core`** e **80% no total**, travada por threshold no Jest.
9. Código **100% tipado**: `strict: true`, sem `any` implícito ou explícito.
10. **Exemplo de padrão:** o cadastro de bancos é o primeiro CRUD completo e serve de modelo para as outras telas. Não há CRUD de usuário, porque não há login.

## <a id="stack"></a>Stack
Todas open source. As versões são o mínimo aceito.

| Grupo | Bibliotecas |
|---|---|
| Core | React ^18.3, TypeScript ^5.5, Webpack ^5.94, React Router DOM ^6.26 |
| Webpack | webpack-cli, webpack-dev-server, webpack-merge, html-webpack-plugin, mini-css-extract-plugin, css-loader, style-loader, sass-loader e sass, postcss-loader e autoprefixer, babel-loader com @babel/preset-env, @babel/preset-react e @babel/preset-typescript, asset modules, dotenv-webpack, copy-webpack-plugin, fork-ts-checker-webpack-plugin, @pmmmwh/react-refresh-webpack-plugin |
| Estado | Zustand com Immer (estado da tela), TanStack Query (dados da API, cache e fetching) |
| Estilo | Tailwind CSS ^3.4, clsx e tailwind-merge, Radix UI ^1.x, lucide-react |
| Testes | Jest ^30, ts-jest ^29.4, jest-environment-jsdom, @testing-library/react, @testing-library/jest-dom, @testing-library/user-event, jest-watch-typeahead, @types/jest, msw, jest-html-reporter, jest-sonar-reporter |
| Qualidade | ESLint ^9 com typescript-eslint, eslint-plugin-react, eslint-plugin-react-hooks, eslint-plugin-jest, eslint-plugin-testing-library, Prettier, Husky, lint-staged, commitlint |
| DI e utilitários | tsyringe e reflect-metadata, zod, date-fns, axios ^1.7, react-hook-form, i18next e react-i18next |

**Escolhas onde a lista oferece duas opções:**
- **Transpilação:** `babel-loader`, por causa dos decorators do `tsyringe`. A checagem de tipos fica com `fork-ts-checker-webpack-plugin`. `ts-loader` não é usado.
- **Assets:** asset modules do Webpack 5. `file-loader` não é usado.
- **DI:** `tsyringe`. `inversify` não é usado.
- **Idioma:** i18next fica configurado, mas só com `pt-BR`.

## <a id="camadas"></a>Camadas
```
front/
  src/
    core/            entidades, value objects, erros de domínio e contratos (interfaces)
    application/     casos de uso; dependem só de core
    infrastructure/  axios, repositórios HTTP, DTOs e validação com zod
    presentation/    páginas, componentes, rotas, hooks do TanStack Query e stores do Zustand
    main/            container do tsyringe, rotas e ponto de entrada
  tests/             espelha src/
  webpack/           common, dev e prod
```

| Camada | Pode importar | Não pode importar |
|---|---|---|
| `core` | nada externo, exceto o Either | React, axios, zod, tsyringe |
| `application` | `core` | React, axios, `infrastructure`, `presentation` |
| `infrastructure` | `core`, `application` | `presentation` |
| `presentation` | `application`, `core` | `infrastructure` direto: recebe pelo container |

O ESLint MUST bloquear importações fora dessa tabela.

**Por funcionalidade:** dentro de cada camada, uma pasta por área: `instituicoes`, `massa`, `consentimentos`, `cenarios`.

## <a id="either"></a>Either
Todo caso de uso retorna `Either<Falha, Sucesso>` e nunca lança exceção para erro esperado.

```ts
export type Either<L, R> = Left<L> | Right<R>;

export class CriarInstituicao {
  constructor(@inject(TOKENS.InstituicaoRepository) private readonly repo: InstituicaoRepository) {}

  async executar(dados: NovaInstituicao): Promise<Either<FalhaDeCadastro, Instituicao>> {
    const nome = NomeDeInstituicao.criar(dados.nome);
    if (nome.isLeft()) return left(nome.value);
    return this.repo.salvar(new Instituicao(nome.value, dados.perfil));
  }
}
```

- Erros de rede e de validação viram `Left` na `infrastructure`.
- A `presentation` trata `Left` e `Right` explicitamente, sem `try/catch` em componente.

## Estado
- **Dados que vêm da API:** sempre TanStack Query. Nada de copiar resposta da API para o Zustand.
- **Estado da tela** (filtros, passo do menu, painel aberto): Zustand com Immer.
- **Formulários:** react-hook-form, validados com o mesmo schema zod do DTO.

## Estilo e componentes
- Tailwind em todo estilo; SASS só quando o Tailwind não resolver.
- Componentes interativos (diálogo, menu, toast, abas) partem do Radix UI, pela acessibilidade.
- Classes condicionais com `cn()`, que une `clsx` e `tailwind-merge`.
- Ícones só do `lucide-react`.
- A faixa do ambiente usa cor diferente em `dev` e `hml` ([UI01](../01-requisitos/interface.md#ui01)).

## Testes
| Alvo | Como |
|---|---|
| Entidades e value objects (`core`) | Jest puro, 100% de cobertura |
| Casos de uso | Jest com repositório falso; testar `Left` e `Right` |
| Repositórios HTTP | Jest com `msw` |
| Componentes e páginas | Testing Library com `user-event` e `msw` |
| Fluxos no navegador | Playwright, contra o simulador real ([estratégia](../06-testes/estrategia-de-testes.md)) |

- Busca de elemento por papel e texto acessível (`getByRole`), não por classe.
- Relatórios: `jest-html-reporter` e `jest-sonar-reporter`.

## Qualidade e commits
- **Husky** roda `lint-staged` no pre-commit (ESLint e Prettier nos arquivos alterados de `front/`) e `commitlint` no commit-msg. Os ganchos não são ativados sozinhos pelo `npm install`, porque `core.hooksPath` vale para todas as worktrees e para os commits só de .NET: quem quiser roda `npm run ganchos` uma vez em `front/`.
- **Commits** que alteram `front/` seguem Conventional Commits, por exemplo: `feat(front): cria tela de bancos`. Commits só de documentação ou de back-end não passam pelo commitlint.
- O CI roda, no job `front`, lint, checagem de tipos, testes com threshold de cobertura e build de produção; o job `painel` roda o Playwright contra o simulador do Compose. Sem Node na máquina, os mesmos comandos rodam pela imagem `node:22` no Docker.

## Variáveis de ambiente
Lidas pelo `dotenv-webpack` e validadas com zod ao subir:

| Variável | Uso |
|---|---|
| `SIM_ADMIN_API_URL` | Endereço da API de administração. Vazio por padrão: o front é servido pelo próprio simulador na porta de administração e chama `/admin/...` na mesma origem. |

A marca do ambiente **não** é variável do front: a tela lê `GET /admin/ambiente`, que devolve a mesma `Simulador:MarcaDoAmbiente` do simulador (RN66, RN72). Valor livre; vazio, a tela não mostra marca. A cor vem de uma lista fixa para `dev`, `hml` e `stage`, com uma cor neutra para qualquer outro valor ([UI01](../01-requisitos/interface.md#ui01)).
