import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, comAmbiente, responder } from '../../apoio/api';
import { bancoEstavel, resumo } from '../../apoio/dados';

const BANCO = '/admin/instituicoes/banco-azul';
const azul = { ...bancoEstavel, endereco: 'banco-azul', nome: 'Banco Azul', perfil: 'lento', pronta: false };

describe('Novo banco e edição (UI03 a UI05)', () => {
  beforeEach(() => comAmbiente());

  it('cria só com nome e perfil e abre a edição preenchida pelo servidor (UI03, Q5)', async () => {
    responder('get', '/admin/perfis-de-comportamento', [{ nome: 'estavel' }, { nome: 'lento' }]);
    const criados = capturar('post', '/admin/instituicoes', azul, 201);
    responder('get', BANCO, azul);
    const { usuario } = abrir('/bancos/novo');

    await usuario.click(await screen.findByRole('button', { name: 'Criar' }));
    expect(await screen.findByText('Obrigatório.')).toBeInTheDocument();

    await usuario.type(screen.getByRole('textbox', { name: 'Nome' }), 'Banco Azul');
    await usuario.selectOptions(screen.getByRole('combobox', { name: 'Perfil de comportamento' }), 'lento');
    await usuario.click(screen.getByRole('button', { name: 'Criar' }));

    expect(await screen.findByRole('heading', { name: /Banco Azul/ })).toBeInTheDocument();
    expect(criados[0]!.corpo).toEqual({ nome: 'Banco Azul', perfil: 'lento' });
    expect(screen.getByRole('textbox', { name: 'Marca' })).toHaveValue('Banco Estável');
  });

  it('nome repetido: mostra a recusa sem perder o que foi digitado', async () => {
    responder('get', '/admin/perfis-de-comportamento', [{ nome: 'estavel' }]);
    responder('post', '/admin/instituicoes', { erros: ['nome já usado'] }, 409);
    const { usuario } = abrir('/bancos/novo');

    await usuario.type(await screen.findByRole('textbox', { name: 'Nome' }), 'Banco Estável');
    await usuario.click(screen.getByRole('button', { name: 'Criar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('nome já usado');
    expect(screen.getByRole('textbox', { name: 'Nome' })).toHaveValue('Banco Estável');
  });

  it('SLA dentro e fora por classe (UI04) e salvar o comportamento', async () => {
    const lento = {
      ...azul,
      sla: [
        { classe: 'alta', slaMs: 1500, p95Ms: 2500, dentroDoSla: false },
        { classe: 'media', slaMs: 2000, p95Ms: 1000, dentroDoSla: true },
      ],
    };
    responder('get', BANCO, lento);
    const alterados = capturar('patch', BANCO, lento);
    const { usuario } = abrir('/bancos/banco-azul');

    await usuario.click(await screen.findByRole('tab', { name: 'Comportamento' }));
    expect(screen.getAllByLabelText('Fora do SLA')).toHaveLength(1);
    expect(screen.getAllByLabelText('Dentro do SLA')).toHaveLength(1);

    const pesos = screen.getByRole('textbox', { name: /Pesos por status/ });
    await usuario.clear(pesos);
    await usuario.type(pesos, '500=1');
    await usuario.click(screen.getByRole('button', { name: 'Salvar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Pesos por status');

    await usuario.clear(pesos);
    await usuario.type(pesos, '500:1, 504:2');
    await usuario.type(screen.getByRole('spinbutton', { name: 'Janela: duração (min)' }), '2');
    await usuario.type(screen.getByRole('spinbutton', { name: 'Janela: intervalo (min)' }), '60');
    await usuario.click(screen.getByRole('button', { name: 'Salvar' }));

    await waitFor(() => expect(alterados).toHaveLength(1));
    expect(alterados[0]!.corpo).toEqual({
      comportamento: expect.objectContaining({
        pesosPorStatus: { '500': 1, '504': 2 },
        janela: { duracaoMin: 2, intervaloMin: 60 },
      }),
    });
    expect(await screen.findByText('Salvo.')).toBeInTheDocument();
  });

  it('limite abaixo do oficial é aceito e avisado, com o oficial ao lado (UI05, RN26)', async () => {
    const comAviso = { ...azul, avisos: [{ campo: 'capacidadeTps', oficial: 300, configurado: 100 }] };
    responder('get', BANCO, azul);
    const alterados = capturar('patch', BANCO, comAviso);
    const { usuario } = abrir('/bancos/banco-azul');

    await usuario.click(await screen.findByRole('tab', { name: 'Limites' }));
    const capacidade = screen.getByRole('spinbutton', { name: 'Capacidade (TPS)' });
    await usuario.clear(capacidade);
    await usuario.type(capacidade, '100');
    await usuario.clear(screen.getByRole('spinbutton', { name: /TPM alta/ }));
    await usuario.type(screen.getByRole('spinbutton', { name: /TPM alta/ }), '500');
    responder('get', BANCO, comAviso);
    await usuario.click(screen.getByRole('button', { name: 'Salvar' }));

    await waitFor(() => expect(alterados).toHaveLength(1));
    expect(alterados[0]!.corpo).toEqual({
      limites: expect.objectContaining({
        capacidadeTps: 100,
        tpm: { mediaAlta: 2000, media: 1500, baixa: 1000, alta: 500 },
      }),
    });
    expect(await screen.findByText('Abaixo do oficial (300)')).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent('capacidadeTps: 100 (Oficial: 300)');
  });

  it('dados gerais e histórico (RN71)', async () => {
    responder('get', BANCO, azul);
    const alterados = capturar('patch', BANCO, azul);
    responder('get', `${BANCO}/historico`, [
      {
        instituicao: 'banco-azul',
        data: '2026-11-10T10:00:00Z',
        autor: 'Diego',
        acao: 'alterada',
        mudancas: [{ campo: 'nome', antes: 'Azul', depois: 'Banco Azul' }],
      },
    ]);
    const { usuario } = abrir('/bancos/banco-azul');

    const produtos = await screen.findByRole('textbox', { name: 'Produtos' });
    await usuario.clear(produtos);
    await usuario.type(produtos, 'contas, cartoes,');
    await usuario.selectOptions(screen.getByRole('combobox', { name: 'Público' }), 'PJ');
    await usuario.click(screen.getByRole('button', { name: 'Salvar' }));
    await waitFor(() => expect(alterados).toHaveLength(1));
    expect(alterados[0]!.corpo).toEqual({
      nome: 'Banco Azul',
      marca: 'Banco Estável',
      publico: 'PJ',
      produtos: ['contas', 'cartoes'],
    });

    await usuario.click(screen.getByRole('tab', { name: 'Histórico' }));
    const tabela = await screen.findByRole('table', { name: 'Histórico' });
    expect(within(tabela).getByText('Diego')).toBeInTheDocument();
    expect(within(tabela).getByText('nome: Azul → Banco Azul')).toBeInTheDocument();
  });

  it('liga anomalia e a lista passa a mostrar o aviso (RN113, RN114)', async () => {
    responder('get', BANCO, azul);
    responder('get', '/admin/cenarios/anomalias', [{ nome: 'revogado-com-403', descricao: 'Troca 401 por 403' }]);
    responder('get', '/admin/cenarios/anomalias/banco-azul', [{ nome: 'revogado-com-403', ligada: false }]);
    const ligadas = capturar('post', '/admin/cenarios/anomalias/banco-azul/revogado-com-403/ligar', {
      nome: 'revogado-com-403',
      ligada: true,
    });
    const { usuario } = abrir('/bancos/banco-azul');

    await usuario.click(await screen.findByRole('tab', { name: 'Anomalias' }));
    expect(await screen.findByText('Troca 401 por 403')).toBeInTheDocument();
    responder('get', '/admin/cenarios/anomalias/banco-azul', [{ nome: 'revogado-com-403', ligada: true }]);
    await usuario.click(screen.getByRole('button', { name: 'Ligar revogado-com-403' }));
    expect(await screen.findByRole('button', { name: 'Desligar revogado-com-403' })).toBeInTheDocument();
    expect(ligadas).toHaveLength(1);

    responder('get', '/admin/instituicoes', [resumo('banco-azul', 'Banco Azul', ['revogado-com-403'])]);
    await usuario.click(screen.getByRole('link', { name: 'Bancos' }));
    expect(await screen.findByRole('img', { name: 'Anomalias ligadas: revogado-com-403' })).toBeInTheDocument();
  });

  it('relógio: fixar, acelerar e voltar ao real (UC28)', async () => {
    const fixado = {
      ...azul,
      relogio: { baseSimulada: '2030-01-01T00:00:00Z', baseReal: '2026-11-10T00:00:00Z', fator: 0 },
    };
    responder('get', BANCO, azul);
    const fixados = capturar('put', `${BANCO}/relogio`, fixado);
    const reais = capturar('delete', `${BANCO}/relogio`, azul);
    const { usuario } = abrir('/bancos/banco-azul');

    await usuario.click(await screen.findByRole('tab', { name: 'Relógio' }));
    expect(screen.getByText('Relógio real.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Voltar ao relógio real' })).toBeDisabled();

    // Campo de data e hora (revisão da 010): o valor local vira ISO UTC no pedido.
    fireEvent.change(screen.getByLabelText(/Fixar em/), { target: { value: '2030-01-01T00:00:00' } });
    responder('get', BANCO, fixado);
    await usuario.click(screen.getByRole('button', { name: 'Fixar' }));
    expect(await screen.findByText(/^Fixado em /)).toBeInTheDocument();

    responder('get', BANCO, { ...fixado, relogio: { ...fixado.relogio, fator: 60 } });
    await usuario.click(screen.getByRole('button', { name: 'Acelerar' }));
    expect(await screen.findByText(/^Acelerado 60×/)).toBeInTheDocument();

    responder('get', BANCO, azul);
    await usuario.click(screen.getByRole('button', { name: 'Voltar ao relógio real' }));
    expect(await screen.findByText('Relógio real.')).toBeInTheDocument();
    expect(fixados.map((p) => p.corpo)).toEqual([
      // 00:00 em São Paulo (UTC-3) é 03:00 em UTC.
      { fixadoEm: '2030-01-01T03:00:00.000Z' },
      { fator: 60 },
    ]);
    expect(reais).toHaveLength(1);
  });

  it('apagar pede confirmação; inativado avisa e reativa; apagado volta à lista', async () => {
    responder('get', BANCO, azul);
    responder('delete', BANCO, { resultado: 'inativada' });
    const { usuario } = abrir('/bancos/banco-azul');

    await usuario.click(await screen.findByRole('button', { name: 'Apagar' }));
    const dialogo = await screen.findByRole('dialog', { name: 'Apagar Banco Azul?' });
    responder('get', BANCO, { ...azul, situacao: 'inativa' });
    await usuario.click(within(dialogo).getByRole('button', { name: 'Confirmar' }));
    expect(await screen.findByText('Banco inativado.')).toBeInTheDocument();

    const reativados = capturar('post', `${BANCO}/reativar`, azul);
    responder('get', BANCO, azul);
    await usuario.click(await screen.findByRole('button', { name: 'Reativar' }));
    await waitFor(() => expect(reativados).toHaveLength(1));

    responder('delete', BANCO, { resultado: 'apagada' });
    responder('get', '/admin/instituicoes', []);
    await usuario.click(screen.getByRole('button', { name: 'Apagar' }));
    await usuario.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Confirmar' }));
    expect(await screen.findByText('Nenhum banco.')).toBeInTheDocument();
  });

  it('banco inexistente mostra o erro', async () => {
    responder('get', '/admin/instituicoes/sumiu', { erros: ['instituição não encontrada'] }, 404);
    abrir('/bancos/sumiu');
    const aviso = await screen.findByRole('alert');
    expect(aviso).toHaveTextContent('Não há banco com o endereço sumiu.');
    expect(aviso).not.toHaveTextContent('instituição não encontrada');
  });
});
