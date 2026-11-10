import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DOS_CENARIOS } from '@/application/cenarios/casos';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Campo } from '../comum/campo';
import { Botao } from '../comum/botao';
import { Confirmacao } from '../comum/confirmacao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Tabela, Titulo } from '../comum/titulo';
import { dataHora } from '../comum/datas';
import { CHAVES } from './chaves';

/** Pacotes de cenário (RN19): ligar ou desligar vale para todas as regras; com regras, não apaga. */
export const Pacotes = () => {
  const { t } = useTranslation();
  const listar = useCaso(TOKENS_DOS_CENARIOS.ListarPacotes);
  const criar = useCaso(TOKENS_DOS_CENARIOS.CriarPacote);
  const ligar = useCaso(TOKENS_DOS_CENARIOS.LigarPacote);
  const apagar = useCaso(TOKENS_DOS_CENARIOS.ApagarPacote);
  const consulta = useConsulta(CHAVES.pacotes, () => listar.executar());
  const { register, handleSubmit, reset } = useForm<{ nome: string }>({ defaultValues: { nome: '' } });
  const criacao = useAcao((nome: string) => criar.executar(nome), {
    invalidar: [CHAVES.pacotes],
    aoTerminar: () => reset(),
  });
  const ligacao = useAcao((pedido: { nome: string; ligado: boolean }) => ligar.executar(pedido), {
    invalidar: [CHAVES.pacotes, CHAVES.regras],
  });
  const remocao = useAcao((nome: string) => apagar.executar(nome), { invalidar: [CHAVES.pacotes] });

  return (
    <>
      <Titulo
        ajuda="pacotes"
        acoes={
          <Link className="rounded border border-slate-300 bg-white px-3 py-1.5 text-sm" to="/cenarios">
            {t('cenarios.pacote.voltar')}
          </Link>
        }
      >
        {t('cenarios.pacote.titulo')}
      </Titulo>
      <Secao titulo={t('cenarios.pacote.criar')}>
        <p className="mb-3 text-sm text-slate-600">{t('cenarios.pacote.explicacao')}</p>
        <form onSubmit={handleSubmit((dados) => criacao.executar(dados.nome))} className="flex items-end gap-2">
          <Campo className="w-72" rotulo={t('cenarios.pacote.nome')} {...register('nome')} />
          <Botao type="submit" disabled={criacao.ocupado}>
            {t('cenarios.pacote.criar')}
          </Botao>
        </form>
        <div className="mt-3">
          <ListaDeErros falha={criacao.falha} />
        </div>
      </Secao>
      <Secao titulo={t('cenarios.pacote.titulo')}>
        <div className="mb-3">
          <ListaDeErros falha={ligacao.falha ?? remocao.falha} />
        </div>
        <Resultado consulta={consulta}>
          {(pacotes) =>
            pacotes.length === 0 ? (
              <p>{t('cenarios.pacote.vazio')}</p>
            ) : (
              <Tabela rotulo={t('cenarios.pacote.titulo')}>
                <thead>
                  <tr>
                    <th>{t('cenarios.pacote.colunas.nome')}</th>
                    <th>{t('cenarios.pacote.colunas.criadoEm')}</th>
                    <th>{t('cenarios.pacote.colunas.situacao')}</th>
                    <th>{t('cenarios.pacote.colunas.acoes')}</th>
                  </tr>
                </thead>
                <tbody>
                  {pacotes.map((pacote) => (
                    <tr key={pacote.nome}>
                      <td className="font-mono text-xs">{pacote.nome}</td>
                      <td>{dataHora(pacote.criadoEm)}</td>
                      <td>{pacote.ligado ? t('cenarios.pacote.ligado') : t('cenarios.pacote.desligado')}</td>
                      <td>
                        <div className="flex gap-2">
                          <Botao
                            variante={pacote.ligado ? 'secundario' : 'primario'}
                            disabled={ligacao.ocupado}
                            aria-label={t(pacote.ligado ? 'cenarios.pacote.desligar' : 'cenarios.pacote.ligar', {
                              nome: pacote.nome,
                            })}
                            onClick={() => ligacao.executar({ nome: pacote.nome, ligado: !pacote.ligado })}
                          >
                            {pacote.ligado ? t('comum.desligar') : t('comum.ligar')}
                          </Botao>
                          <Confirmacao
                            gatilho={t('comum.apagar')}
                            titulo={t('cenarios.pacote.apagar.titulo', { nome: pacote.nome })}
                            aoConfirmar={() => remocao.executar(pacote.nome)}
                          >
                            {t('cenarios.pacote.apagar.texto')}
                          </Confirmacao>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Tabela>
            )
          }
        </Resultado>
      </Secao>
    </>
  );
};
