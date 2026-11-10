import { useId } from 'react';
import { TOKENS_DOS_CENARIOS } from '@/application/cenarios/casos';
import { useCaso } from '../comum/casos';
import { useConsulta } from '../comum/consultas';
import { CHAVES } from './chaves';

/** Sugestões de `operationId` do catálogo para o campo de endpoint; sem catálogo, o campo continua livre. */
export const useSugestoesDeEndpoint = () => {
  const id = useId();
  const operacoes = useCaso(TOKENS_DOS_CENARIOS.Operacoes);
  const consulta = useConsulta(CHAVES.operacoes, () => operacoes.executar());
  const lista = consulta.data?.isRight() ? consulta.data.value : [];
  return {
    lista: id,
    sugestoes: (
      <datalist id={id}>
        {lista.map((operationId) => (
          <option key={operationId} value={operationId} />
        ))}
      </datalist>
    ),
  };
};
