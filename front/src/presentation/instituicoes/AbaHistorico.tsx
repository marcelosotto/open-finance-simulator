import { useTranslation } from 'react-i18next';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import { useCaso } from '../comum/casos';
import { Resultado, useConsulta } from '../comum/consultas';
import { Secao, Tabela } from '../comum/titulo';
import { dataHora } from '../comum/datas';
import { CHAVES } from './chaves';

/** Do mais novo ao mais antigo, com data, autor, antes e depois (RN71). A ordem vem da API. */
export const AbaHistorico = ({ endereco }: { readonly endereco: string }) => {
  const { t } = useTranslation();
  const historico = useCaso(TOKENS_DAS_INSTITUICOES.Historico);
  const consulta = useConsulta(CHAVES.historico(endereco), () => historico.executar(endereco));
  return (
    <Secao titulo={t('instituicoes.abas.historico')}>
      <Resultado consulta={consulta}>
        {(entradas) =>
          entradas.length === 0 ? (
            <p>{t('instituicoes.historico.vazio')}</p>
          ) : (
            <Tabela rotulo={t('instituicoes.abas.historico')}>
              <thead>
                <tr>
                  <th>{t('instituicoes.historico.data')}</th>
                  <th>{t('instituicoes.historico.autor')}</th>
                  <th>{t('instituicoes.historico.acao')}</th>
                  <th>{t('instituicoes.historico.mudancas')}</th>
                </tr>
              </thead>
              <tbody>
                {entradas.map((entrada, indice) => (
                  <tr key={`${entrada.data}-${indice}`}>
                    <td title={entrada.data}>{dataHora(entrada.data)}</td>
                    <td>{entrada.autor}</td>
                    <td>{entrada.acao}</td>
                    <td>
                      <ul>
                        {entrada.mudancas.map((mudanca, i) => (
                          <li key={`${mudanca.campo}-${i}`} className="break-all font-mono text-xs">
                            {mudanca.campo}: {mudanca.antes ?? '—'} → {mudanca.depois ?? '—'}
                          </li>
                        ))}
                      </ul>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Tabela>
          )
        }
      </Resultado>
    </Secao>
  );
};
