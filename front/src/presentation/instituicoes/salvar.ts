import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import type { AlteracaoDeInstituicao } from '@/core/instituicoes/instituicao';
import { useCaso } from '../comum/casos';
import { useAcao } from '../comum/consultas';
import { CHAVES } from './chaves';

/** PATCH do banco, relendo lista e detalhe depois de salvar. */
export const useSalvarBanco = (endereco: string) => {
  const { t } = useTranslation();
  const alterar = useCaso(TOKENS_DAS_INSTITUICOES.Alterar);
  const [aviso, setAviso] = useState<string | null>(null);
  const acao = useAcao((alteracao: AlteracaoDeInstituicao) => alterar.executar({ endereco, alteracao }), {
    invalidar: [CHAVES.lista, CHAVES.detalhe(endereco), CHAVES.historico(endereco)],
    aoTerminar: () => setAviso(t('comum.salvo')),
  });
  return { ...acao, aviso };
};
