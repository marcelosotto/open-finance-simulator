import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DA_MASSA } from '@/application/massa/casos';
import type { MassaDoCliente } from '@/core/massa/massa';
import { useCaso } from '../comum/casos';
import { Resultado, useConsulta } from '../comum/consultas';
import { Secao, Tabela, Titulo } from '../comum/titulo';
import { dataHora } from '../comum/datas';
import { CHAVES, classeDoLink } from './chaves';
import { Estado } from '../comum/rotulos';

const Dados = ({ massa }: { readonly massa: MassaDoCliente }) => {
  const { t } = useTranslation();
  const { cliente } = massa;
  return (
    <Secao titulo={t('massa.cliente.dados')}>
      <dl className="grid grid-cols-[max-content_1fr] gap-x-4 gap-y-1 text-sm">
        <dt className="font-medium">{t('massa.cliente.documento')}</dt>
        <dd className="font-mono">{cliente.documento}</dd>
        <dt className="font-medium">{t('massa.cliente.tipo')}</dt>
        <dd>{cliente.tipo}</dd>
        <dt className="font-medium">{t('massa.cliente.nome')}</dt>
        <dd>{cliente.nome}</dd>
        <dt className="font-medium">{t('massa.cliente.marca')}</dt>
        <dd>{cliente.marca}</dd>
        <dt className="font-medium">{t('massa.cliente.natureza')}</dt>
        <dd>{cliente.natureza}</dd>
        <dt className="font-medium">{t('massa.cliente.lote')}</dt>
        <dd>
          {cliente.loteId === null ? (
            t('massa.cliente.semLote')
          ) : (
            <Link className={classeDoLink} to={`/massa/lotes/${encodeURIComponent(cliente.loteId)}`}>
              {cliente.loteId}
            </Link>
          )}
        </dd>
      </dl>
    </Secao>
  );
};

const Recursos = ({ massa }: { readonly massa: MassaDoCliente }) => {
  const { t } = useTranslation();
  return (
    <Secao titulo={t('massa.cliente.recursos')}>
      {massa.recursos.length === 0 ? (
        <p>{t('massa.cliente.semRecursos')}</p>
      ) : (
        <Tabela rotulo={t('massa.cliente.recursos')}>
          <thead>
            <tr>
              <th>{t('massa.cliente.colunasDeRecursos.resourceId')}</th>
              <th>{t('massa.cliente.colunasDeRecursos.banco')}</th>
              <th>{t('massa.cliente.colunasDeRecursos.tipo')}</th>
              <th>{t('massa.cliente.colunasDeRecursos.produto')}</th>
              <th>{t('massa.cliente.colunasDeRecursos.estado')}</th>
            </tr>
          </thead>
          <tbody>
            {massa.recursos.map((recurso) => (
              <tr key={recurso.resourceId}>
                <td className="font-mono text-xs">{recurso.resourceId}</td>
                <td>{recurso.banco}</td>
                <td>{recurso.tipo}</td>
                <td>{recurso.produto}</td>
                <td>
                  <Estado codigo={recurso.estado} />
                </td>
              </tr>
            ))}
          </tbody>
        </Tabela>
      )}
    </Secao>
  );
};

/** Cada consentimento leva à visão de consentimento (Q7). */
const Consentimentos = ({ massa }: { readonly massa: MassaDoCliente }) => {
  const { t } = useTranslation();
  return (
    <Secao titulo={t('massa.cliente.consentimentos')}>
      {massa.consentimentos.length === 0 ? (
        <p>{t('massa.cliente.semConsentimentos')}</p>
      ) : (
        <Tabela rotulo={t('massa.cliente.consentimentos')}>
          <thead>
            <tr>
              <th>{t('massa.cliente.colunasDeConsentimentos.consentId')}</th>
              <th>{t('massa.cliente.colunasDeConsentimentos.banco')}</th>
              <th>{t('massa.cliente.colunasDeConsentimentos.estado')}</th>
              <th>{t('massa.cliente.colunasDeConsentimentos.permissoes')}</th>
              <th>{t('massa.cliente.colunasDeConsentimentos.criadoEm')}</th>
            </tr>
          </thead>
          <tbody>
            {massa.consentimentos.map((consentimento) => (
              <tr key={consentimento.consentId}>
                <td>
                  <Link
                    className={`${classeDoLink} font-mono text-xs`}
                    to={`/consentimentos/${encodeURIComponent(consentimento.consentId)}`}
                  >
                    {consentimento.consentId}
                  </Link>
                </td>
                <td>{consentimento.banco}</td>
                <td>
                  <Estado codigo={consentimento.estado} />
                </td>
                <td className="break-all font-mono text-xs">{consentimento.permissoes.join(', ')}</td>
                <td title={consentimento.criadoEm}>{dataHora(consentimento.criadoEm)}</td>
              </tr>
            ))}
          </tbody>
        </Tabela>
      )}
    </Secao>
  );
};

export const Cliente = () => {
  const { t } = useTranslation();
  const { documento = '' } = useParams();
  const obter = useCaso(TOKENS_DA_MASSA.Cliente);
  const consulta = useConsulta(CHAVES.cliente(documento), () => obter.executar(documento));
  return (
    <>
      <Titulo ajuda="cliente">{t('massa.cliente.titulo', { documento })}</Titulo>
      <Resultado consulta={consulta} seInexistente={t('massa.cliente.inexistente', { documento })}>
        {(massa) => (
          <>
            <Dados massa={massa} />
            <Recursos massa={massa} />
            <Consentimentos massa={massa} />
          </>
        )}
      </Resultado>
    </>
  );
};
