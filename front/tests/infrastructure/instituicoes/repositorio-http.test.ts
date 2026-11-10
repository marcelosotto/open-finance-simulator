import { ClienteAdmin } from '@/infrastructure/http/cliente-admin';
import { RepositorioDeInstituicoesHttp } from '@/infrastructure/instituicoes/repositorio-http';
import { RepositorioDoAmbienteHttp } from '@/infrastructure/ambiente/repositorio-http';
import { API } from '../../apoio/renderizar';
import { capturar, responder } from '../../apoio/api';
import { bancoEstavel, resumo } from '../../apoio/dados';

const repositorio = () => new RepositorioDeInstituicoesHttp(new ClienteAdmin(API, () => ''));

describe('Repositório HTTP das instituições', () => {
  it('lista, obtém, cria, altera e reativa', async () => {
    responder('get', '/admin/instituicoes', [resumo('banco-estavel', 'Banco Estável')]);
    responder('get', '/admin/instituicoes/banco-estavel', bancoEstavel);
    const criados = capturar('post', '/admin/instituicoes', bancoEstavel, 201);
    const alterados = capturar('patch', '/admin/instituicoes/banco-estavel', bancoEstavel);
    responder('post', '/admin/instituicoes/banco-estavel/reativar', bancoEstavel);

    const r = repositorio();
    expect((await r.listar()).value).toEqual([resumo('banco-estavel', 'Banco Estável')]);
    expect((await r.obter('banco-estavel')).isRight()).toBe(true);
    await r.criar({ nome: 'Banco Azul', perfil: 'lento' });
    await r.alterar('banco-estavel', { limites: { capacidadeTps: 100 } });
    expect((await r.reativar('banco-estavel')).isRight()).toBe(true);

    expect(criados[0]!.corpo).toEqual({ nome: 'Banco Azul', perfil: 'lento' });
    expect(alterados[0]!.corpo).toEqual({ limites: { capacidadeTps: 100 } });
  });

  it('perfis, remoção e histórico', async () => {
    responder('get', '/admin/perfis-de-comportamento', [{ nome: 'estavel', comportamento: {} }, { nome: 'lento' }]);
    responder('delete', '/admin/instituicoes/banco-azul', { resultado: 'inativada' });
    responder('get', '/admin/instituicoes/banco-azul/historico', [
      { instituicao: 'banco-azul', data: '2026-11-10T10:00:00Z', autor: 'Diego', acao: 'criada', mudancas: [] },
    ]);

    const r = repositorio();
    expect((await r.perfis()).value).toEqual(['estavel', 'lento']);
    expect((await r.apagar('banco-azul')).value).toBe('inativada');
    expect((await r.historico('banco-azul')).value).toEqual([
      { data: '2026-11-10T10:00:00Z', autor: 'Diego', acao: 'criada', mudancas: [] },
    ]);
  });

  it('anomalias juntam o catálogo ao estado do banco, e o erro de qualquer lado sobe', async () => {
    responder('get', '/admin/cenarios/anomalias', [{ nome: 'revogado-com-403', descricao: 'Troca 401 por 403' }]);
    responder('get', '/admin/cenarios/anomalias/banco-estavel', [
      { nome: 'revogado-com-403', ligada: true },
      { nome: 'nova', ligada: false },
    ]);
    responder('post', '/admin/cenarios/anomalias/banco-estavel/revogado-com-403/desligar', {
      nome: 'revogado-com-403',
      ligada: false,
    });

    const r = repositorio();
    expect((await r.anomalias('banco-estavel')).value).toEqual([
      { nome: 'revogado-com-403', ligada: true, descricao: 'Troca 401 por 403' },
      { nome: 'nova', ligada: false, descricao: '' },
    ]);
    expect((await r.ligarAnomalia('banco-estavel', 'revogado-com-403', false)).value).toEqual({
      nome: 'revogado-com-403',
      ligada: false,
      descricao: '',
    });

    responder('get', '/admin/cenarios/anomalias/x', { erros: ['instituição não encontrada'] }, 404);
    expect((await r.anomalias('x')).isLeft()).toBe(true);
    responder('get', '/admin/cenarios/anomalias', { erros: ['fora'] }, 500);
    expect((await r.anomalias('banco-estavel')).isLeft()).toBe(true);
  });

  it('relógio: PUT para fixar ou acelerar, DELETE para voltar ao real', async () => {
    const fixados = capturar('put', '/admin/instituicoes/banco-estavel/relogio', bancoEstavel);
    const reais = capturar('delete', '/admin/instituicoes/banco-estavel/relogio', bancoEstavel);

    const r = repositorio();
    await r.mudarRelogio('banco-estavel', { fixadoEm: '2030-01-01T00:00:00Z' });
    await r.mudarRelogio('banco-estavel', null);

    expect(fixados[0]!.corpo).toEqual({ fixadoEm: '2030-01-01T00:00:00Z' });
    expect(reais).toHaveLength(1);
  });

  it('ambiente', async () => {
    responder('get', '/admin/ambiente', { marca: 'hml' });
    const resultado = await new RepositorioDoAmbienteHttp(new ClienteAdmin(API, () => '')).obter();
    expect(resultado.isRight() && resultado.value.cor).toBe('hml');
  });
});
