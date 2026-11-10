// jsdom sem fetch, Request, Response e streams do Node, que o msw precisa. Copia os globais do Node para a janela.
const { TestEnvironment } = require('jest-environment-jsdom');

const DO_NODE = [
  'fetch',
  'Request',
  'Response',
  'Headers',
  'FormData',
  'Blob',
  'File',
  'ReadableStream',
  'WritableStream',
  'TransformStream',
  'TextEncoder',
  'TextDecoder',
  'BroadcastChannel',
  'MessageChannel',
  'MessagePort',
  'structuredClone',
  'AbortController',
  'AbortSignal',
  'URL',
  'URLSearchParams',
];

class AmbienteJsdom extends TestEnvironment {
  constructor(...args) {
    super(...args);
    for (const nome of DO_NODE) {
      if (globalThis[nome] !== undefined) this.global[nome] = globalThis[nome];
    }
  }
}

module.exports = AmbienteJsdom;
