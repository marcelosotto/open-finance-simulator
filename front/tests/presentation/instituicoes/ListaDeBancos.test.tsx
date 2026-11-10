import { screen } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { comAmbiente, responder } from '../../apoio/api';
import { resumo } from '../../apoio/dados';

describe('Lista de bancos (UI02)', () => {
  beforeEach(() => comAmbiente());

  it('mostra perfil e situação, e o aviso só no banco com anomalia ligada (RN114)', async () => {
    responder('get', '/admin/instituicoes', [
      resumo('banco-estavel', 'Banco Estável'),
      resumo('banco-instavel', 'Banco Instável', ['revogado-com-403']),
    ]);

    abrir('/bancos');

    expect(await screen.findByRole('link', { name: 'Banco Estável' })).toBeInTheDocument();
    const avisos = screen.getAllByRole('img', { name: /Anomalias ligadas/ });
    expect(avisos).toHaveLength(1);
    expect(avisos[0]).toHaveAccessibleName('Anomalias ligadas: revogado-com-403');
    expect(screen.getAllByText('Ativa')).toHaveLength(2);
  });

  it('mostra o erro e deixa tentar de novo quando a administração falha', async () => {
    responder('get', '/admin/instituicoes', { erros: ['instabilidade'] }, 500);

    const { usuario } = abrir('/bancos');

    expect(await screen.findByRole('alert')).toHaveTextContent('instabilidade');
    responder('get', '/admin/instituicoes', [resumo('banco-estavel', 'Banco Estável')]);
    await usuario.click(screen.getByRole('button', { name: 'Tentar de novo' }));
    expect(await screen.findByRole('link', { name: 'Banco Estável' })).toBeInTheDocument();
  });

  it('lista vazia', async () => {
    responder('get', '/admin/instituicoes', []);
    abrir('/bancos');
    expect(await screen.findByText('Nenhum banco.')).toBeInTheDocument();
  });
});
