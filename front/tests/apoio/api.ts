import { http, HttpResponse, type JsonBodyType } from 'msw';
import { API } from './renderizar';
import { servidor } from './servidor';

type Metodo = 'get' | 'post' | 'put' | 'patch' | 'delete';

/** Responde uma rota da administração com JSON (status 200 por padrão). */
export const responder = (metodo: Metodo, caminho: string, corpo: JsonBodyType, status = 200) =>
  servidor.use(http[metodo](`${API}${caminho}`, () => HttpResponse.json(corpo, { status })));

/** Guarda os pedidos da rota (corpo e cabeçalhos) e responde com JSON. */
export const capturar = (metodo: Metodo, caminho: string, corpo: JsonBodyType, status = 200) => {
  const pedidos: { corpo: unknown; cabecalhos: Headers; url: URL }[] = [];
  servidor.use(
    http[metodo](`${API}${caminho}`, async ({ request }) => {
      const texto = await request.text();
      pedidos.push({
        corpo: texto === '' ? undefined : JSON.parse(texto),
        cabecalhos: request.headers,
        url: new URL(request.url),
      });
      return HttpResponse.json(corpo, { status });
    }),
  );
  return pedidos;
};

/** A marca de ambiente que todo layout lê. */
export const comAmbiente = (marca = 'hml') => responder('get', '/admin/ambiente', { marca, portaOpenFinance: 8080 });
