import { useTranslation } from 'react-i18next';
import type { EfeitoDaRegra, RegraDeCenario } from '@/core/cenarios/regra';
import { dataHora } from '../comum/datas';

/** Textos de uma regra para a lista e o painel da vencedora. */
export const useDescricaoDaRegra = () => {
  const { t } = useTranslation();

  const rotulo = (regra: Pick<RegraDeCenario, 'id' | 'nome'>): string => regra.nome ?? regra.id;

  const efeito = (e: EfeitoDaRegra): string => {
    const partes = [
      e.latenciaMs !== null && t('cenarios.detalhe.latencia', { ms: e.latenciaMs }),
      e.status !== null && t('cenarios.detalhe.status', { status: e.status }),
      e.resultado !== null && t(`cenarios.resultados.${e.resultado}`, { defaultValue: e.resultado }),
      e.falha !== null && t(`cenarios.falhasDeTransporte.${e.falha}`, { defaultValue: e.falha }),
      e.estado,
      e.defeito,
      e.campo !== null && t('cenarios.detalhe.campo', { campo: e.campo }),
      e.quantidade !== null && t('cenarios.detalhe.quantidade', { quantidade: e.quantidade }),
      e.esperaMs !== null && t('cenarios.detalhe.espera', { ms: e.esperaMs }),
    ].filter((parte): parte is string => typeof parte === 'string' && parte.length > 0);
    const nome = t(`cenarios.efeitos.${e.tipo}`, { defaultValue: e.tipo });
    return partes.length === 0 ? nome : `${nome}: ${partes.join(', ')}`;
  };

  const filtros = (regra: RegraDeCenario): string =>
    (['instituicao', 'endpoint', 'cliente', 'recurso'] as const)
      .flatMap((campo) => {
        const valor = regra[campo];
        return valor === null ? [] : [t(`cenarios.filtro.${campo}`, { valor })];
      })
      .join(', ');

  const periodo = (regra: Pick<RegraDeCenario, 'inicio' | 'fim'>): string => {
    const { inicio, fim } = regra;
    if (inicio !== null && fim !== null)
      return t('cenarios.periodo.entre', { inicio: dataHora(inicio), fim: dataHora(fim) });
    if (inicio !== null) return t('cenarios.periodo.desde', { inicio: dataHora(inicio) });
    if (fim !== null) return t('cenarios.periodo.ate', { fim: dataHora(fim) });
    return t('cenarios.periodo.sempre');
  };

  return { rotulo, efeito, filtros, periodo };
};
