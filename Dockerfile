# syntax=docker/dockerfile:1.7
# Imagem do simulador com as specs travadas embutidas (ADR-002): sobe sem rede.

# Build do back-office (spec 010, Q3): o simulador serve o resultado na porta de administração.
FROM node:22 AS front
WORKDIR /front
COPY front/package.json front/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY front/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig specs.lock.json ./
COPY config/ config/
COPY src/ src/
COPY tools/ tools/
RUN dotnet restore src/Simulador.Host/Simulador.Host.csproj \
 && dotnet restore tools/Simulador.Ferramentas/Simulador.Ferramentas.csproj

# Baixa o commit travado e confere os SHA-256. O token é opcional (repositório público).
RUN --mount=type=secret,id=specs_token,required=false \
    export SPECS_TOKEN="$(cat /run/secrets/specs_token 2>/dev/null || true)" \
 && dotnet run --project tools/Simulador.Ferramentas -c Release -- specs baixar \
 && dotnet run --project tools/Simulador.Ferramentas -c Release --no-build -- specs verificar

RUN dotnet publish src/Simulador.Host/Simulador.Host.csproj -c Release -o /app --no-restore \
 && mkdir -p /app/specs \
 && cp -r .specs/*/. /app/specs/

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
COPY --from=front /front/dist ./backoffice
ENV ASPNETCORE_HTTP_PORTS="" \
    Simulador__PastaDasSpecs=/app/specs \
    Simulador__PortaOpenFinance=8080 \
    Simulador__PortaAdministracao=8081
EXPOSE 8080 8081
USER $APP_UID
ENTRYPOINT ["dotnet", "Simulador.Host.dll"]
