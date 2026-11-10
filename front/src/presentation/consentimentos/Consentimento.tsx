import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DOS_CONSENTIMENTOS } from '@/application/consentimentos/casos';
import { podeDecidir, podeRevogar, type VisaoDoConsentimento } from '@/core/consentimentos/consentimento';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Botao } from '../comum/botao';
import { Confirmacao } from '../comum/confirmacao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Tabela, Titulo } from '../comum/titulo';
import { dataHora } from '../comum/datas';
import { CHAVES } from './chaves';

/** UI14: quantas aprovações faltam, com aprovar e recusar só quando há pendência válida (RN98, RN99). */
const Alcada = ({ visao, aoMudar }: { readonly visao: VisaoDoConsentimento; readonly aoMudar: () => void }) => {
  const { t } = useTranslation();
  const decidir = useCaso(TOKENS_DOS_CONSENTIMENTOS.Decidir);
  // Em 409 (decisão concorrente, prazo vencido) a tela relê a visão (edge case da spec).
  const acao = useAcao((decisao: 'aprovar' | 'recusar') => decidir.executar({ consentId: visao.consentId, decisao }), {
    invalidar: [CHAVES.visao(visao.consentId)],
  });
  const alcada = visao.alcada;
  return (
    <Secao titulo={t('consentimentos.visao.alcada')}>
      {alcada === null ? (
        <p>{t('consentimentos.visao.semAlcada')}</p>
      ) : (
        <div className="space-y-2">
          <p>{t('consentimentos.visao.situacao', { situacao: alcada.situacao })}</p>
          <p role="status" className="font-semibold">
            {t('consentimentos.visao.faltam', { n: alcada.aprovacoesPendentes })}
          </p>
          {alcada.prazo !== null && <p>{t('consentimentos.visao.prazoDaAlcada', { prazo: dataHora(alcada.prazo) })}</p>}
          {podeDecidir(visao) && (
            <div className="flex gap-2">
              <Botao disabled={acao.ocupado} onClick={() => acao.executar('aprovar')}>
                {t('consentimentos.visao.aprovar')}
              </Botao>
              <Botao variante="perigo" disabled={acao.ocupado} onClick={() => acao.executar('recusar')}>
                {t('consentimentos.visao.recusar')}
              </Botao>
            </div>
          )}
          <ListaDeErros falha={acao.falha} />
          {acao.falha?.tipo === 'conflito' && (
            <Botao variante="secundario" onClick={aoMudar}>
              {t('comum.tentarDeNovo')}
            </Botao>
          )}
        </div>
      )}
      {visao.decisoes.length > 0 && (
        <Tabela rotulo={t('consentimentos.visao.decisoes')}>
          <tbody>
            {visao.decisoes.map((d, i) => (
              <tr key={`${d.em}-${i}`}>
                <td>{d.autor}</td>
                <td>{d.decisao}</td>
                <td title={d.em}>{dataHora(d.em)}</td>
              </tr>
            ))}
          </tbody>
        </Tabela>
      )}
    </Secao>
  );
};

const Revogacao = ({ consentId }: { readonly consentId: string }) => {
  const { t } = useTranslation();
  const revogar = useCaso(TOKENS_DOS_CONSENTIMENTOS.Revogar);
  const acao = useAcao((id: string) => revogar.executar(id), { invalidar: [CHAVES.visao(consentId)] });
  return (
    <div className="flex flex-col items-end gap-2">
      <Confirmacao
        gatilho={t('consentimentos.visao.revogar')}
        titulo={t('consentimentos.visao.revogarTitulo')}
        aoConfirmar={() => acao.executar(consentId)}
      >
        {t('consentimentos.visao.revogarTexto')}
      </Confirmacao>
      <ListaDeErros falha={acao.falha} />
    </div>
  );
};

/** A visão admin: estado efetivo e recursos pelo relógio do banco, sem a regra de cenário (RN97). */
export const Consentimento = () => {
  const { t } = useTranslation();
  const { consentId = '' } = useParams();
  const obter = useCaso(TOKENS_DOS_CONSENTIMENTOS.Obter);
  const consulta = useConsulta(CHAVES.visao(consentId), () => obter.executar(consentId));
  return (
    <Resultado consulta={consulta}>
      {(visao) => (
        <>
          <Titulo acoes={podeRevogar(visao) ? <Revogacao consentId={visao.consentId} /> : undefined}>
            {t('consentimentos.visao.titulo', { id: visao.consentId })}
          </Titulo>
          <Secao titulo={t('consentimentos.visao.estado')}>
            <dl className="grid grid-cols-[max-content_1fr] gap-x-4 gap-y-1 text-sm">
              <dt>{t('consentimentos.visao.estado')}</dt>
              <dd data-testid="estado" className="font-semibold">
                {visao.estado}
              </dd>
              <dt>{t('consentimentos.visao.cliente')}</dt>
              <dd>{visao.cliente}</dd>
              <dt>{t('consentimentos.visao.banco')}</dt>
              <dd>{visao.banco}</dd>
              <dt>{t('consentimentos.visao.receptora')}</dt>
              <dd className="font-mono text-xs">{visao.receptora ?? '—'}</dd>
              <dt>{t('consentimentos.visao.criadoEm')}</dt>
              <dd title={visao.criadoEm}>{dataHora(visao.criadoEm)}</dd>
              <dt>{t('consentimentos.visao.expiraEm')}</dt>
              <dd>{visao.expiraEm === null ? t('consentimentos.visao.semFim') : dataHora(visao.expiraEm)}</dd>
              {visao.rejeitadoPor !== null && (
                <>
                  <dt>{t('consentimentos.visao.rejeitadoPor')}</dt>
                  <dd>
                    {visao.rejeitadoPor} {visao.razao !== null && `(${visao.razao})`}
                  </dd>
                </>
              )}
              <dt>{t('consentimentos.visao.permissoes')}</dt>
              <dd className="font-mono text-xs">{visao.permissoes.join(', ')}</dd>
            </dl>
          </Secao>
          <Alcada visao={visao} aoMudar={() => consulta.refetch()} />
          <Secao titulo={t('consentimentos.visao.recursos')}>
            <Tabela rotulo={t('consentimentos.visao.recursos')}>
              <thead>
                <tr>
                  <th>{t('consentimentos.visao.recurso')}</th>
                  <th>{t('consentimentos.visao.tipo')}</th>
                  <th>{t('consentimentos.visao.estado')}</th>
                </tr>
              </thead>
              <tbody>
                {visao.recursos.map((r) => (
                  <tr key={r.resourceId}>
                    <td className="font-mono text-xs">{r.resourceId}</td>
                    <td>{r.tipo}</td>
                    <td>{r.estado}</td>
                  </tr>
                ))}
              </tbody>
            </Tabela>
          </Secao>
        </>
      )}
    </Resultado>
  );
};
