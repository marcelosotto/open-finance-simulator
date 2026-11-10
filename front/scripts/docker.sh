#!/bin/sh
# Roda npm dentro da imagem node:22, sem Node na máquina (spec 010, Q4).
# node_modules fica num volume nomeado, para não gravar binários Linux na árvore local.
# Uso: sh front/scripts/docker.sh ci | sh front/scripts/docker.sh run test:cov
set -e
FRONT=$(cd "$(dirname "$0")/.." && (pwd -W 2>/dev/null || pwd))
MSYS_NO_PATHCONV=1 exec docker run --rm \
  -v "$FRONT:/app" \
  -v sim-front-node-modules:/app/node_modules \
  -w /app \
  ${SIM_DOCKER_REDE:+--network "$SIM_DOCKER_REDE"} \
  -e CI=true \
  node:22 npm "$@"
