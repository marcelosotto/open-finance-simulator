import type { Either } from '@/core/comum/either';
import type { Falha } from '@/core/comum/falha';
import {
  formularioDaRegra,
  formularioVazio,
  montarPedido,
  nomeDePacote,
  pedidoDeVencedora,
  type FormularioDeRegra,
} from '@/core/cenarios/formulario';
import type { TipoDeEfeito } from '@/core/cenarios/regra';
import { efeito, regra } from './regras';

const formulario = (parcial: Partial<FormularioDeRegra> = {}): FormularioDeRegra => ({
  ...formularioVazio(),
  instituicao: 'banco-estavel',
  ...parcial,
});

const erros = <T>(resultado: Either<Falha, T>): readonly string[] => (resultado.isLeft() ? resultado.value.erros : []);

const valor = <T>(resultado: Either<Falha, T>): T => {
  if (resultado.isLeft()) throw new Error(resultado.value.erros.join(', '));
  return resultado.value;
};

describe('montarPedido', () => {
  it('leva só os campos do efeito e omite os vazios', () => {
    const pedido = valor(
      montarPedido(
        formulario({
          tipo: 'latencia',
          latenciaMs: ' 3000 ',
          status: '500',
          falha: 'json-invalido',
          cliente: '  ',
        }),
      ),
    );
    expect(pedido).toEqual({
      instituicao: 'banco-estavel',
      efeito: { tipo: 'latencia', latenciaMs: 3000 },
      frequenciaPct: 100,
      ligada: true,
    });
    expect(Object.keys(pedido)).not.toContain('cliente');
  });

  it('leva todos os filtros, período, semente, pacote e nome informados', () => {
    const pedido = valor(
      montarPedido(
        formulario({
          nome: 'lento',
          endpoint: 'accountsGetAccounts',
          cliente: '76109277673',
          recurso: 'acc-1',
          tipo: 'timeout',
          frequenciaPct: '12,5',
          inicio: '2026-12-01T00:00:00Z',
          fim: '2026-12-31T23:59:59Z',
          semente: '-42',
          pacote: 'natal',
          ligada: false,
        }),
      ),
    );
    expect(pedido).toEqual({
      nome: 'lento',
      instituicao: 'banco-estavel',
      endpoint: 'accountsGetAccounts',
      cliente: '76109277673',
      recurso: 'acc-1',
      efeito: { tipo: 'timeout' },
      frequenciaPct: 12.5,
      inicio: '2026-12-01T00:00:00Z',
      fim: '2026-12-31T23:59:59Z',
      semente: -42,
      ligada: false,
      pacote: 'natal',
    });
  });

  it('frequência vazia fica de fora (o servidor usa 100)', () => {
    expect(valor(montarPedido(formulario({ latenciaMs: '0', frequenciaPct: '' }))).frequenciaPct).toBeUndefined();
  });

  it.each([
    ['status', { status: '500', esperaMs: '2000' }, { status: 500, esperaMs: 2000 }],
    ['status', { status: '503' }, { status: 503 }],
    ['timeout', { esperaMs: '15000' }, { esperaMs: 15000 }],
    ['indisponibilidade', { resultado: '504', esperaMs: '100' }, { resultado: '504', esperaMs: 100 }],
    ['transporte', { falha: 'corpo-truncado' }, { falha: 'corpo-truncado' }],
    ['estado', { estado: 'UNAVAILABLE' }, { estado: 'UNAVAILABLE' }],
    [
      'defeito',
      { defeito: 'total-records-incoerente', campo: 'x', quantidade: '3' },
      { defeito: 'total-records-incoerente' },
    ],
    [
      'defeito',
      { defeito: 'campo-obrigatorio-ausente', campo: 'transactionAmount', quantidade: '3' },
      { defeito: 'campo-obrigatorio-ausente', campo: 'transactionAmount' },
    ],
    ['defeito', { defeito: 'formato-invalido', campo: '' }, { defeito: 'formato-invalido' }],
    [
      'defeito',
      { defeito: 'dado-muda-na-paginacao', campo: 'x', quantidade: '-5' },
      { defeito: 'dado-muda-na-paginacao', quantidade: -5 },
    ],
    ['defeito', { defeito: 'dado-muda-na-paginacao', quantidade: '' }, { defeito: 'dado-muda-na-paginacao' }],
  ] as const)('efeito %s %o', (tipo, campos, esperado) => {
    const pedido = valor(montarPedido(formulario({ tipo, ...campos })));
    expect(pedido.efeito).toEqual({ tipo, ...esperado });
  });

  it.each([
    ['latencia', { latenciaMs: '' }, 'LATENCIA_INVALIDA'],
    ['latencia', { latenciaMs: '-1' }, 'LATENCIA_INVALIDA'],
    ['latencia', { latenciaMs: '1.5' }, 'LATENCIA_INVALIDA'],
    ['status', { status: '' }, 'STATUS_INVALIDO'],
    ['status', { status: '99' }, 'STATUS_INVALIDO'],
    ['status', { status: '600' }, 'STATUS_INVALIDO'],
    ['status', { status: '500', esperaMs: '0' }, 'ESPERA_INVALIDA'],
    ['timeout', { esperaMs: 'abc' }, 'ESPERA_INVALIDA'],
    ['indisponibilidade', { resultado: '' }, 'RESULTADO_OBRIGATORIO'],
    ['indisponibilidade', { resultado: '500' }, 'RESULTADO_OBRIGATORIO'],
    ['transporte', { falha: '' }, 'FALHA_OBRIGATORIA'],
    ['estado', { estado: 'OUTRO' }, 'ESTADO_OBRIGATORIO'],
    ['defeito', { defeito: ' ' }, 'DEFEITO_OBRIGATORIO'],
    ['defeito', { defeito: 'dado-muda-na-paginacao', quantidade: '0' }, 'QUANTIDADE_DO_DEFEITO_INVALIDA'],
    ['defeito', { defeito: 'dado-muda-na-paginacao', quantidade: '1001' }, 'QUANTIDADE_DO_DEFEITO_INVALIDA'],
    ['defeito', { defeito: 'dado-muda-na-paginacao', quantidade: '-1001' }, 'QUANTIDADE_DO_DEFEITO_INVALIDA'],
    ['defeito', { defeito: 'dado-muda-na-paginacao', quantidade: 'muitos' }, 'QUANTIDADE_DO_DEFEITO_INVALIDA'],
  ] as const)('recusa %s %o com %s', (tipo, campos, codigo) => {
    const resultado = montarPedido(formulario({ tipo, ...campos }));
    expect(resultado.isLeft() && resultado.value.tipo).toBe('validacao');
    expect(erros(resultado)).toEqual([codigo]);
  });

  it('aceita a quantidade nos extremos', () => {
    const extremos = ['1000', '-1000'].map(
      (quantidade) =>
        valor(montarPedido(formulario({ tipo: 'defeito', defeito: 'dado-muda-na-paginacao', quantidade }))).efeito
          .quantidade,
    );
    expect(extremos).toEqual([1000, -1000]);
  });

  it.each(['-1', '100.5', '101', 'muito'])('recusa a frequência %s', (frequenciaPct) => {
    expect(erros(montarPedido(formulario({ latenciaMs: '1', frequenciaPct })))).toEqual([
      'FREQUENCIA_DA_REGRA_INVALIDA',
    ]);
  });

  it('aceita frequência 0 e 100', () => {
    expect(valor(montarPedido(formulario({ latenciaMs: '1', frequenciaPct: '0' }))).frequenciaPct).toBe(0);
    expect(valor(montarPedido(formulario({ latenciaMs: '1', frequenciaPct: '100' }))).frequenciaPct).toBe(100);
  });

  it('regra de estado exige frequência de 100', () => {
    const base = { tipo: 'estado', estado: 'UNAVAILABLE' } as const;
    expect(erros(montarPedido(formulario({ ...base, frequenciaPct: '50' })))).toEqual(['ESTADO_EXIGE_FREQUENCIA_100']);
    expect(valor(montarPedido(formulario({ ...base, frequenciaPct: '100' }))).frequenciaPct).toBe(100);
    expect(valor(montarPedido(formulario({ ...base, frequenciaPct: '' }))).frequenciaPct).toBeUndefined();
  });

  it('junta todos os erros de formato de uma vez', () => {
    const resultado = montarPedido(
      formulario({ latenciaMs: '', frequenciaPct: '200', inicio: 'ontem', fim: 'amanhã', semente: '4.2' }),
    );
    expect(erros(resultado)).toEqual([
      'LATENCIA_INVALIDA',
      'FREQUENCIA_DA_REGRA_INVALIDA',
      'INICIO_INVALIDO',
      'FIM_INVALIDO',
      'SEMENTE_DA_REGRA_INVALIDA',
    ]);
  });

  it('recusa um tipo de efeito fora da lista', () => {
    const resultado = montarPedido(formulario({ tipo: 'sumir' as TipoDeEfeito }));
    expect(erros(resultado)).toEqual(['TIPO_DE_EFEITO_INVALIDO']);
  });

  it('não confere o alcance nem o endpoint: isso a administração recusa', () => {
    expect(valor(montarPedido(formulario({ instituicao: '', latenciaMs: '1' })))).toEqual({
      efeito: { tipo: 'latencia', latenciaMs: 1 },
      frequenciaPct: 100,
      ligada: true,
    });
  });
});

describe('formularioDaRegra', () => {
  it('traz a regra do servidor para o formulário e volta ao mesmo pedido', () => {
    const original = regra({
      nome: 'paginação muda',
      endpoint: 'accountsGetAccountsAccountIdTransactions',
      cliente: '76109277673',
      recurso: 'acc-1',
      efeito: efeito({ tipo: 'defeito', latenciaMs: null, defeito: 'dado-muda-na-paginacao', quantidade: 2 }),
      frequenciaPct: 10,
      inicio: '2026-12-01T00:00:00+00:00',
      fim: '2026-12-31T00:00:00+00:00',
      semente: 42,
      ligada: false,
      pacote: 'natal',
    });

    const f = formularioDaRegra(original);

    expect(f).toMatchObject({
      nome: 'paginação muda',
      tipo: 'defeito',
      defeito: 'dado-muda-na-paginacao',
      quantidade: '2',
      latenciaMs: '',
      frequenciaPct: '10',
      semente: '42',
      ligada: false,
      pacote: 'natal',
    });
    expect(valor(montarPedido(f))).toEqual({
      nome: 'paginação muda',
      instituicao: 'banco-estavel',
      endpoint: 'accountsGetAccountsAccountIdTransactions',
      cliente: '76109277673',
      recurso: 'acc-1',
      efeito: { tipo: 'defeito', defeito: 'dado-muda-na-paginacao', quantidade: 2 },
      frequenciaPct: 10,
      inicio: '2026-12-01T00:00:00+00:00',
      fim: '2026-12-31T00:00:00+00:00',
      semente: 42,
      ligada: false,
      pacote: 'natal',
    });
  });

  it('tipo desconhecido do servidor abre como latência', () => {
    expect(formularioDaRegra(regra({ efeito: efeito({ tipo: 'novo' }) })).tipo).toBe('latencia');
  });
});

describe('pedidoDeVencedora', () => {
  it('exige banco e endpoint', () => {
    expect(erros(pedidoDeVencedora({ instituicao: ' ', endpoint: '', cliente: '', recurso: '' }))).toEqual([
      'BANCO_DA_CHAMADA_OBRIGATORIO',
      'ENDPOINT_DA_CHAMADA_OBRIGATORIO',
    ]);
    expect(erros(pedidoDeVencedora({ instituicao: 'banco-estavel', endpoint: '', cliente: '', recurso: '' }))).toEqual([
      'ENDPOINT_DA_CHAMADA_OBRIGATORIO',
    ]);
    expect(erros(pedidoDeVencedora({ instituicao: '', endpoint: 'x', cliente: '', recurso: '' }))).toEqual([
      'BANCO_DA_CHAMADA_OBRIGATORIO',
    ]);
  });

  it('cliente e recurso só quando informados', () => {
    expect(
      valor(
        pedidoDeVencedora({ instituicao: 'banco-estavel', endpoint: 'accountsGetAccounts', cliente: '', recurso: '' }),
      ),
    ).toEqual({ instituicao: 'banco-estavel', endpoint: 'accountsGetAccounts' });
    expect(
      valor(
        pedidoDeVencedora({
          instituicao: 'banco-estavel',
          endpoint: 'accountsGetAccounts',
          cliente: ' 1 ',
          recurso: 'acc',
        }),
      ),
    ).toEqual({ instituicao: 'banco-estavel', endpoint: 'accountsGetAccounts', cliente: '1', recurso: 'acc' });
  });
});

describe('nomeDePacote', () => {
  it('apara e exige o nome', () => {
    expect(valor(nomeDePacote('  natal '))).toBe('natal');
    expect(erros(nomeDePacote('   '))).toEqual(['NOME_DO_PACOTE_OBRIGATORIO']);
  });
});
