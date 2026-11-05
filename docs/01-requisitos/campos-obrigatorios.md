# Registro de Campos Obrigatórios

Nos cadastros do simulador, tudo tem valor padrão. Um campo só é obrigatório quando não existe padrão que faça sentido. Os campos das APIs oficiais seguem as specs.

| Cadastro | Campo | Por que é obrigatório |
|---|---|---|
| Instituição | Nome | Identifica o banco nas telas e nos registros de chamada. |
| Perfil de comportamento | Nome | Identifica o modelo na escolha. |
| Perfil de dados | Nome | Identifica o modelo na escolha. |
| Regra de cenário | Alcance e efeito | Sem isso, a regra não faz nada. |
| Cliente simulado | Instituição | Todo cliente pertence a uma instituição. |
| Lote | Quantidade | Sem isso, não se sabe quanto gerar. |

Gerados pelo simulador quando não informados: documento, nome, conta, `organisationId`, semente e identificadores.
