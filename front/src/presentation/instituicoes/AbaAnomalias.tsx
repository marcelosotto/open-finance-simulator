import { useTranslation } from 'react-i18next';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Tabela } from '../comum/titulo';
import { CHAVES } from './chaves';

/** Liga e desliga as anomalias do catálogo no banco (RN113); a lista acompanha (RN114). */
export const AbaAnomalias = ({ endereco }: { readonly endereco: string }) => {
  const { t } = useTranslation();
  const anomalias = useCaso(TOKENS_DAS_INSTITUICOES.Anomalias);
  const ligar = useCaso(TOKENS_DAS_INSTITUICOES.LigarAnomalia);
  const consulta = useConsulta(CHAVES.anomalias(endereco), () => anomalias.executar(endereco));
  const acao = useAcao((pedido: { nome: string; ligada: boolean }) => ligar.executar({ endereco, ...pedido }), {
    invalidar: [CHAVES.anomalias(endereco), CHAVES.lista, CHAVES.detalhe(endereco), CHAVES.historico(endereco)],
  });
  return (
    <Secao titulo={t('instituicoes.abas.anomalias')}>
      <ListaDeErros falha={acao.falha} />
      <Resultado consulta={consulta}>
        {(lista) =>
          lista.length === 0 ? (
            <p>{t('instituicoes.anomalias.nenhuma')}</p>
          ) : (
            <Tabela rotulo={t('instituicoes.abas.anomalias')}>
              <tbody>
                {lista.map((anomalia) => (
                  <tr key={anomalia.nome}>
                    <td className="font-mono text-xs">{anomalia.nome}</td>
                    <td>{anomalia.descricao}</td>
                    <td>{anomalia.ligada ? t('comum.ligada') : t('comum.desligada')}</td>
                    <td>
                      <Botao
                        variante={anomalia.ligada ? 'secundario' : 'primario'}
                        disabled={acao.ocupado}
                        aria-label={`${anomalia.ligada ? t('comum.desligar') : t('comum.ligar')} ${anomalia.nome}`}
                        onClick={() => acao.executar({ nome: anomalia.nome, ligada: !anomalia.ligada })}
                      >
                        {anomalia.ligada ? t('comum.desligar') : t('comum.ligar')}
                      </Botao>
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
