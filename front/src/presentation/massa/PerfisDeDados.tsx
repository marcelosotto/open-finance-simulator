import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { TOKENS_DA_MASSA } from '@/application/massa/casos';
import { perfilPronto, type PerfilDeDados } from '@/core/massa/massa';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Campo } from '../comum/campo';
import { Botao } from '../comum/botao';
import { Confirmacao } from '../comum/confirmacao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Tabela, Titulo } from '../comum/titulo';
import { CHAVES, separar } from './chaves';

interface Formulario {
  nome: string;
  transacoesMin: string;
  transacoesMax: string;
  valorMin: string;
  valorMax: string;
  saldoInicial: string;
  tiposDeLancamento: string;
  produtos: string;
}

const NUMEROS = ['transacoesMin', 'transacoesMax', 'valorMin', 'valorMax', 'saldoInicial'] as const;

const VAZIO: Formulario = {
  nome: '',
  transacoesMin: '0',
  transacoesMax: '4',
  valorMin: '10',
  valorMax: '2000',
  saldoInicial: '5000',
  tiposDeLancamento: 'PIX, TED, BOLETO, CARTAO',
  produtos: 'contas',
};

const paraFormulario = (perfil: PerfilDeDados): Formulario => ({
  nome: perfil.nome,
  transacoesMin: String(perfil.transacoesMin),
  transacoesMax: String(perfil.transacoesMax),
  valorMin: String(perfil.valorMin),
  valorMax: String(perfil.valorMax),
  saldoInicial: String(perfil.saldoInicial),
  tiposDeLancamento: perfil.tiposDeLancamento.join(', '),
  produtos: perfil.produtos.join(', '),
});

const paraPerfil = (dados: Formulario): PerfilDeDados => ({
  nome: dados.nome,
  transacoesMin: Number(dados.transacoesMin),
  transacoesMax: Number(dados.transacoesMax),
  valorMin: Number(dados.valorMin),
  valorMax: Number(dados.valorMax),
  saldoInicial: Number(dados.saldoInicial),
  tiposDeLancamento: separar(dados.tiposDeLancamento),
  produtos: separar(dados.produtos),
});

/** Lista prontos e criados; os prontos não têm editar nem apagar (US3, cenário 4). */
export const PerfisDeDados = () => {
  const { t } = useTranslation();
  const listar = useCaso(TOKENS_DA_MASSA.Perfis);
  const salvar = useCaso(TOKENS_DA_MASSA.SalvarPerfil);
  const apagar = useCaso(TOKENS_DA_MASSA.ApagarPerfil);
  const consulta = useConsulta(CHAVES.perfis, () => listar.executar());
  const [editando, setEditando] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const { register, handleSubmit, reset } = useForm<Formulario>({ defaultValues: VAZIO });

  const acaoDeSalvar = useAcao((dados: Formulario) => salvar.executar(paraPerfil(dados)), {
    invalidar: [CHAVES.perfis],
    aoTerminar: () => {
      setAviso(t('comum.salvo'));
      setEditando(null);
      reset(VAZIO);
    },
  });
  const acaoDeApagar = useAcao((nome: string) => apagar.executar(nome), {
    invalidar: [CHAVES.perfis],
    aoTerminar: () => setAviso(t('massa.perfis.apagado')),
  });

  const editar = (perfil: PerfilDeDados) => {
    setEditando(perfil.nome);
    setAviso(null);
    acaoDeSalvar.limpar();
    reset(paraFormulario(perfil));
  };

  return (
    <>
      <Titulo ajuda="perfis">{t('massa.perfis.titulo')}</Titulo>
      <Secao titulo={t('massa.perfis.lista')}>
        <ListaDeErros falha={acaoDeApagar.falha} />
        {aviso !== null && (
          <p role="status" className="mb-2 text-sm text-teal-800">
            {aviso}
          </p>
        )}
        <Resultado consulta={consulta}>
          {(perfis) => (
            <Tabela rotulo={t('massa.perfis.lista')}>
              <thead>
                <tr>
                  <th>{t('massa.perfis.colunas.nome')}</th>
                  <th>{t('massa.perfis.colunas.tipo')}</th>
                  <th>{t('massa.perfis.colunas.transacoes')}</th>
                  <th>{t('massa.perfis.colunas.valores')}</th>
                  <th>{t('massa.perfis.colunas.saldoInicial')}</th>
                  <th>{t('massa.perfis.colunas.produtos')}</th>
                  <th>{t('massa.perfis.colunas.acoes')}</th>
                </tr>
              </thead>
              <tbody>
                {perfis.map((perfil) => (
                  <tr key={perfil.nome}>
                    <td className="font-mono text-xs">{perfil.nome}</td>
                    <td>{perfilPronto(perfil.nome) ? t('massa.perfis.pronto') : t('massa.perfis.criado')}</td>
                    <td>
                      {perfil.transacoesMin} a {perfil.transacoesMax}
                    </td>
                    <td>
                      {perfil.valorMin} a {perfil.valorMax}
                    </td>
                    <td>{perfil.saldoInicial}</td>
                    <td>{perfil.produtos.join(', ')}</td>
                    <td>
                      {perfilPronto(perfil.nome) ? (
                        <span className="text-xs text-slate-600">{t('massa.perfis.prontoNaoMuda')}</span>
                      ) : (
                        <div className="flex gap-2">
                          <Botao variante="secundario" onClick={() => editar(perfil)}>
                            {t('massa.perfis.editar', { nome: perfil.nome })}
                          </Botao>
                          <Confirmacao
                            gatilho={t('massa.perfis.apagar', { nome: perfil.nome })}
                            titulo={t('massa.perfis.apagarTitulo', { nome: perfil.nome })}
                            aoConfirmar={() => acaoDeApagar.executar(perfil.nome)}
                            desabilitado={acaoDeApagar.ocupado}
                          >
                            {t('massa.perfis.apagarTexto')}
                          </Confirmacao>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </Tabela>
          )}
        </Resultado>
      </Secao>
      <Secao titulo={editando === null ? t('massa.perfis.novo') : t('massa.perfis.edicao', { nome: editando })}>
        <form
          onSubmit={handleSubmit((dados) => {
            setAviso(null);
            acaoDeSalvar.executar(dados);
          })}
          className="grid max-w-2xl gap-3 sm:grid-cols-2"
        >
          <Campo
            className="sm:col-span-2"
            rotulo={t('massa.perfis.campos.nome')}
            readOnly={editando !== null}
            {...register('nome')}
          />
          {NUMEROS.map((campo) => (
            <Campo
              key={campo}
              type="number"
              step="any"
              rotulo={t(`massa.perfis.campos.${campo}`)}
              {...register(campo)}
            />
          ))}
          <Campo
            className="sm:col-span-2"
            rotulo={t('massa.perfis.campos.tiposDeLancamento')}
            {...register('tiposDeLancamento')}
          />
          <Campo className="sm:col-span-2" rotulo={t('massa.perfis.campos.produtos')} {...register('produtos')} />
          <div className="sm:col-span-2">
            <ListaDeErros falha={acaoDeSalvar.falha} />
          </div>
          <div className="flex gap-2 sm:col-span-2">
            <Botao type="submit" disabled={acaoDeSalvar.ocupado}>
              {t('comum.salvar')}
            </Botao>
            {editando !== null && (
              <Botao
                variante="secundario"
                onClick={() => {
                  setEditando(null);
                  acaoDeSalvar.limpar();
                  reset(VAZIO);
                }}
              >
                {t('comum.cancelar')}
              </Botao>
            )}
          </div>
        </form>
      </Secao>
    </>
  );
};
