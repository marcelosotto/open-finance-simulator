#!/bin/sh
# Roda o Playwright pela imagem oficial, na rede do Compose, contra o simulador real (spec 010, Q10).
# Pré-requisito: o Compose no ar (docker compose up --build). Rede e endereço podem ser trocados:
#   SIM_DOCKER_REDE=<rede do compose> SIM_BACKOFFICE_URL=http://simulador:8081 sh front/scripts/playwright.sh
set -e
FRONT=$(cd "$(dirname "$0")/.." && (pwd -W 2>/dev/null || pwd))
REDE=${SIM_DOCKER_REDE:-$(docker network ls --format '{{.Name}}' | grep -m1 '_default$')}
VERSAO=$(sed -n 's/.*"@playwright\/test": "^\{0,1\}\([0-9.]*\)".*/\1/p' "$FRONT/package.json" | head -1)
VERSAO=$(MSYS_NO_PATHCONV=1 docker run --rm -v "$FRONT:/app" -v sim-front-node-modules:/app/node_modules -w /app node:22 \
  node -p "require('@playwright/test/package.json').version" 2>/dev/null || echo "$VERSAO")
MSYS_NO_PATHCONV=1 exec docker run --rm --ipc=host \
  --network "$REDE" \
  -v "$FRONT:/app" \
  -v sim-front-node-modules:/app/node_modules \
  -w /app \
  -e CI=true \
  -e SIM_BACKOFFICE_URL="${SIM_BACKOFFICE_URL:-http://simulador:8081}" \
  "mcr.microsoft.com/playwright:v$VERSAO-noble" npx playwright test "$@"
