import { rotuloDoCliente } from '@/core/massa/massa';

describe('Rótulo do cliente (UI19)', () => {
  it('mostra nome, documento e PF ou PJ', () => {
    expect(rotuloDoCliente({ nome: 'Mariana Alves', documento: '95000000080', tipo: 'PF' })).toBe(
      'Mariana Alves — 95000000080 (PF)',
    );
  });
});
