import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TriangleAlert } from 'lucide-react';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import { temAnomalia } from '@/core/instituicoes/instituicao';
import { useCaso } from '../comum/casos';
import { Resultado, useConsulta } from '../comum/consultas';
import { Tabela, Titulo } from '../comum/titulo';
import { CHAVES } from './chaves';

/** UI02: perfil, situação e aviso quando há anomalia ligada (RN114). */
export const ListaDeBancos = () => {
  const { t } = useTranslation();
  const listar = useCaso(TOKENS_DAS_INSTITUICOES.Listar);
  const consulta = useConsulta(CHAVES.lista, () => listar.executar());
  return (
    <>
      <Titulo
        acoes={
          <Link className="rounded bg-teal-700 px-3 py-1.5 text-sm font-medium text-white" to="/bancos/novo">
            {t('instituicoes.novo')}
          </Link>
        }
      >
        {t('instituicoes.titulo')}
      </Titulo>
      <Resultado consulta={consulta}>
        {(bancos) =>
          bancos.length === 0 ? (
            <p>{t('instituicoes.vazio')}</p>
          ) : (
            <Tabela rotulo={t('instituicoes.titulo')}>
              <thead>
                <tr>
                  <th>{t('instituicoes.colunas.nome')}</th>
                  <th>{t('instituicoes.colunas.endereco')}</th>
                  <th>{t('instituicoes.colunas.perfil')}</th>
                  <th>{t('instituicoes.colunas.situacao')}</th>
                  <th>{t('instituicoes.colunas.avisos')}</th>
                </tr>
              </thead>
              <tbody>
                {bancos.map((banco) => (
                  <tr key={banco.endereco}>
                    <td>
                      <Link className="text-teal-800 underline" to={`/bancos/${banco.endereco}`}>
                        {banco.nome}
                      </Link>
                    </td>
                    <td className="font-mono text-xs">{banco.endereco}</td>
                    <td>{banco.perfil}</td>
                    <td>{t(`instituicoes.situacao.${banco.situacao}`)}</td>
                    <td>
                      {temAnomalia(banco) && (
                        <span
                          role="img"
                          aria-label={t('instituicoes.avisoDeAnomalia', { nomes: banco.anomaliasLigadas.join(', ') })}
                          className="inline-flex items-center gap-1 rounded bg-amber-100 px-2 py-0.5 text-xs text-amber-900"
                        >
                          <TriangleAlert className="h-3.5 w-3.5" aria-hidden />
                          {banco.anomaliasLigadas.join(', ')}
                        </span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </Tabela>
          )
        }
      </Resultado>
    </>
  );
};
