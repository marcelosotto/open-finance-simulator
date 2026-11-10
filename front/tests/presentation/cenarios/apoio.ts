import type { JsonBodyType } from 'msw';
import { comAmbiente, responder } from '../../apoio/api';
import { resumo } from '../../apoio/dados';

// Catálogo de defeitos da 009, como GET /admin/cenarios/defeitos devolve.
export const catalogoDeDefeitos = [
  { nome: 'campo-obrigatorio-ausente', descricao: 'Tira um campo obrigatório do primeiro item.' },
  { nome: 'formato-invalido', descricao: 'Reescreve em dd/MM/aaaa um campo de data.' },
  { nome: 'total-records-incoerente', descricao: 'Soma 1 a meta.totalRecords.' },
  { nome: 'dado-muda-na-paginacao', descricao: 'Na página seguinte, acrescenta ou tira itens.' },
];

export const pacoteNatal = { nome: 'natal', ligado: false, criadoEm: '2026-11-05T12:00:00+00:00' };

interface Base {
  readonly regras?: JsonBodyType;
  readonly pacotes?: JsonBodyType;
}

/** Tudo o que as telas de cenários leem ao abrir: ambiente, regras, bancos, catálogo, pacotes e defeitos. */
export const comBase = ({ regras = [], pacotes = [pacoteNatal] }: Base = {}) => {
  comAmbiente();
  responder('get', '/admin/cenarios/regras', regras);
  responder('get', '/admin/instituicoes', [
    resumo('banco-estavel', 'Banco Estável'),
    resumo('banco-instavel', 'Banco Instável'),
  ]);
  responder('get', '/admin/catalogo', {
    commit: 'abc',
    operacoes: [
      { operationId: 'accountsGetAccounts', contrato: 'accounts', metodo: 'GET', caminho: '/accounts' },
      {
        operationId: 'accountsGetAccountsAccountIdTransactions',
        contrato: 'accounts',
        metodo: 'GET',
        caminho: '/accounts/{accountId}/transactions',
      },
    ],
  });
  responder('get', '/admin/cenarios/pacotes', pacotes);
  responder('get', '/admin/cenarios/defeitos', catalogoDeDefeitos);
};
