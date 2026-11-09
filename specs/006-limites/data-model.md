# Data Model: Limites

Todos os contadores ficam no Redis, só com o total. Não há coleção nova.

| Contador | Chave | Validade | Janela |
|---|---|---|---|
| Capacidade | `lim:cap:{banco}:{segundo}` | 5 s | Segundo cheio, hora real |
| Tráfego | `lim:tpm:{banco}:{operationId}:{receptora}:{minuto}` | 120 s | Minuto cheio, hora real |
| Cota | `lim:cota:{banco}:{yyyyMM}:{operationId}:{objeto}:{cliente}:{receptora}` | 70 dias | Mês UTC, relógio da instituição |

`segundo` e `minuto` são os números de segundos e minutos desde 1970.

**Regras**
- Capacidade e tráfego: soma e compara; maior que o limite recusa.
- Cota: lê e compara; maior ou igual ao limite responde 423; soma depois do 2XX.
- Objeto: valor do último parâmetro do caminho; sem parâmetro, `consentId`.
- Cota por classe: `limiteMensal` oficial 420 usa `SaldosELimites`; os demais, a frequência.
- Resultado da chave: só a chave válida dispensa conferência e contagem.
