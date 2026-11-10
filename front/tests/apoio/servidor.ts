import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

// Servidor msw único; cada teste acrescenta os seus handlers com servidor.use(...).
// Padrão: a lista de clientes do banco (UI19) vem vazia, para telas que só a usam como filtro opcional.
export const servidor = setupServer(
  http.get('http://simulador.test/admin/massa/clientes', () =>
    HttpResponse.json({ clientes: [], inicio: 0, quantidade: 1000, temMais: false }),
  ),
  http.get('http://simulador.test/admin/massa/resumo', () =>
    HttpResponse.json({ clientes: 0, consentimentos: 0, recursos: 0, lotes: 0 }),
  ),
);
