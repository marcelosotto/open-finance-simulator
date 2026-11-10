import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DOS_CENARIOS } from '@/application/cenarios/casos';
import { agruparPorAlcance } from '@/core/cenarios/alcance';
import type { RegraDeCenario } from '@/core/cenarios/regra';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Botao } from '../comum/botao';
import { Confirmacao } from '../comum/confirmacao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Tabela, Titulo } from '../comum/titulo';
import { CHAVES } from './chaves';
import { useDescricaoDaRegra } from './descricao';
import { PainelDaVencedora } from './Vencedora';

const link = 'rounded px-3 py-1.5 text-sm font-medium';

const TabelaDeRegras = ({
  rotulo,
  regras,
  ocupado,
  aoLigar,
  aoApagar,
}: {
  readonly rotulo: string;
  readonly regras: readonly RegraDeCenario[];
  readonly ocupado: boolean;
  readonly aoLigar: (regra: RegraDeCenario) => void;
  readonly aoApagar: (regra: RegraDeCenario) => void;
}) => {
  const { t } = useTranslation();
  const descricao = useDescricaoDaRegra();
  return (
    <Tabela rotulo={rotulo}>
      <thead>
        <tr>
          <th>{t('cenarios.colunas.regra')}</th>
          <th>{t('cenarios.colunas.filtros')}</th>
          <th>{t('cenarios.colunas.efeito')}</th>
          <th>{t('cenarios.colunas.frequencia')}</th>
          <th>{t('cenarios.colunas.periodo')}</th>
          <th>{t('cenarios.colunas.pacote')}</th>
          <th>{t('cenarios.colunas.situacao')}</th>
          <th>{t('cenarios.colunas.acoes')}</th>
        </tr>
      </thead>
      <tbody>
        {regras.map((regra) => {
          const nome = descricao.rotulo(regra);
          return (
            <tr key={regra.id}>
              <td>
                <Link
                  className="text-teal-800 underline"
                  to={`/cenarios/regras/${encodeURIComponent(regra.id)}`}
                  title={t('cenarios.editarRegra', { regra: nome })}
                >
                  {nome}
                </Link>
              </td>
              <td className="text-xs">{descricao.filtros(regra)}</td>
              <td>{descricao.efeito(regra.efeito)}</td>
              <td>{`${regra.frequenciaPct}%`}</td>
              <td className="text-xs">{descricao.periodo(regra)}</td>
              <td>{regra.pacote ?? t('cenarios.semPacote')}</td>
              <td>{regra.ligada ? t('comum.ligada') : t('comum.desligada')}</td>
              <td>
                <div className="flex gap-2">
                  <Botao
                    variante={regra.ligada ? 'secundario' : 'primario'}
                    disabled={ocupado}
                    aria-label={t(regra.ligada ? 'cenarios.desligarRegra' : 'cenarios.ligarRegra', { regra: nome })}
                    onClick={() => aoLigar(regra)}
                  >
                    {regra.ligada ? t('comum.desligar') : t('comum.ligar')}
                  </Botao>
                  <Confirmacao
                    gatilho={t('comum.apagar')}
                    titulo={t('cenarios.apagar.titulo', { regra: nome })}
                    aoConfirmar={() => aoApagar(regra)}
                  >
                    {t('cenarios.apagar.texto')}
                  </Confirmacao>
                </div>
              </td>
            </tr>
          );
        })}
      </tbody>
    </Tabela>
  );
};

/** UI06: regras por alcance, com ligar, desligar e apagar, e o painel da vencedora. */
export const Regras = () => {
  const { t } = useTranslation();
  const listar = useCaso(TOKENS_DOS_CENARIOS.ListarRegras);
  const ligar = useCaso(TOKENS_DOS_CENARIOS.LigarRegra);
  const apagar = useCaso(TOKENS_DOS_CENARIOS.ApagarRegra);
  const consulta = useConsulta(CHAVES.regras, () => listar.executar());
  const ligacao = useAcao((pedido: { id: string; ligada: boolean }) => ligar.executar(pedido), {
    invalidar: [CHAVES.regras],
  });
  const remocao = useAcao((id: string) => apagar.executar(id), { invalidar: [CHAVES.regras] });

  return (
    <>
      <Titulo
        acoes={
          <>
            <Link className={`${link} border border-slate-300 bg-white text-slate-800`} to="/cenarios/pacotes">
              {t('cenarios.pacotes')}
            </Link>
            <Link className={`${link} bg-teal-700 text-white`} to="/cenarios/regras/nova">
              {t('cenarios.nova')}
            </Link>
          </>
        }
      >
        {t('cenarios.titulo')}
      </Titulo>
      <div className="mb-3">
        <ListaDeErros falha={ligacao.falha ?? remocao.falha} />
      </div>
      <Resultado consulta={consulta}>
        {(regras) =>
          regras.length === 0 ? (
            <p className="mb-6">{t('cenarios.vazio')}</p>
          ) : (
            agruparPorAlcance(regras).map((grupo) => {
              const titulo = t(`cenarios.alcances.${grupo.alcance}`);
              return (
                <Secao key={grupo.alcance} titulo={titulo}>
                  <TabelaDeRegras
                    rotulo={titulo}
                    regras={grupo.regras}
                    ocupado={ligacao.ocupado || remocao.ocupado}
                    aoLigar={(regra) => ligacao.executar({ id: regra.id, ligada: !regra.ligada })}
                    aoApagar={(regra) => remocao.executar(regra.id)}
                  />
                </Secao>
              );
            })
          )
        }
      </Resultado>
      <PainelDaVencedora />
    </>
  );
};
