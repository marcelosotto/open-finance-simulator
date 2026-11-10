# Quickstart: Back-office

## Subir
```bash
docker compose up --build
```
Abrir `http://localhost:8081/`. A faixa "Ambiente simulado, dados fictícios" aparece sempre; a marca `hml` aparece em destaque (UI01, UI16). Com `Simulador__MarcaDoAmbiente=` a marca some.

## Front sem Node na máquina
```bash
sh front/scripts/docker.sh ci
sh front/scripts/docker.sh run lint
sh front/scripts/docker.sh run typecheck
sh front/scripts/docker.sh run test:cov
sh front/scripts/docker.sh run build
```
Com Node 22 instalado, os mesmos comandos rodam com `npm` dentro de `front/`. Desenvolvimento: `npm run dev` (porta 3000, proxy de `/admin` para `localhost:8081`).

## Playwright
Com o Compose no ar:
```bash
sh front/scripts/playwright.sh
```

## Conferir à mão
1. **Bancos**: criar "Banco Azul" com perfil `lento`; a edição abre preenchida; baixar `capacidadeTps` para 100 → aviso com o oficial 300 (UI05); SLA fora nas quatro classes (UI04).
2. **Anomalia**: ligar `revogado-com-403` no Banco Azul → a lista mostra o aviso (UI02, RN114).
3. **Consentimento**: criar receptora "Coletor", cliente PF; Novo consentimento → passos → `AUTHORISED` (UI10, UI13). Cliente PJ com 2 alçadas → aprovar duas vezes → recursos `AVAILABLE` (UI14).
4. **Massa**: distribuição com bancos 60 + 30 → salvar bloqueado até somar 100 (UI07); gerar lote de 3 e acompanhar.
5. **Cenários**: regra de latência no banco e de status no endpoint → "qual regra vale?" com o mesmo banco e endpoint aponta a de endpoint (UI06, RN15).
