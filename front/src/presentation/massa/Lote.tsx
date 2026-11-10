import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DA_MASSA } from '@/application/massa/casos';
import { emAndamento, type Lote as LoteDaMassa } from '@/core/massa/massa';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Botao } from '../comum/botao';
import { Confirmacao } from '../comum/confirmacao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Tabela, Titulo } from '../comum/titulo';
import { dataHora } from '../comum/datas';
import { CHAVES, classeDoLink } from './chaves';

const TAMANHO_DA_PAGINA = 20;

const ClientesDoLote = ({ id, rodando }: { readonly id: string; readonly rodando: boolean }) => {
  const { t } = useTranslation();
  const clientes = useCaso(TOKENS_DA_MASSA.ClientesDoLote);
  const [inicio, setInicio] = useState(0);
  const consulta = useConsulta(
    CHAVES.clientesDoLote(id, inicio),
    () => clientes.executar({ id, inicio, quantidade: TAMANHO_DA_PAGINA }),
    { intervaloMs: rodando ? 2000 : false },
  );
  return (
    <Secao titulo={t('massa.lote.clientes')}>
      <Resultado consulta={consulta}>
        {(pagina) => (
          <>
            {pagina.length === 0 ? (
              <p>{t('massa.lote.semClientes')}</p>
            ) : (
              <>
                <p className="mb-2 text-sm text-slate-600">
                  {t('massa.lote.pagina', { de: inicio + 1, ate: inicio + pagina.length })}
                </p>
                <Tabela rotulo={t('massa.lote.clientes')}>
                  <thead>
                    <tr>
                      <th>{t('massa.lote.colunas.documento')}</th>
                      <th>{t('massa.lote.colunas.tipo')}</th>
                      <th>{t('massa.lote.colunas.nome')}</th>
                      <th>{t('massa.lote.colunas.natureza')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pagina.map((cliente) => (
                      <tr key={cliente.documento}>
                        <td>
                          <Link
                            className={`${classeDoLink} font-mono text-xs`}
                            to={`/massa/clientes/${encodeURIComponent(cliente.documento)}`}
                          >
                            {cliente.documento}
                          </Link>
                        </td>
                        <td>{cliente.tipo}</td>
                        <td>{cliente.nome}</td>
                        <td>{cliente.natureza}</td>
                      </tr>
                    ))}
                  </tbody>
                </Tabela>
              </>
            )}
            <div className="mt-3 flex gap-2">
              <Botao
                variante="secundario"
                disabled={inicio === 0}
                onClick={() => setInicio((atual) => Math.max(0, atual - TAMANHO_DA_PAGINA))}
              >
                {t('massa.lote.anterior')}
              </Botao>
              <Botao
                variante="secundario"
                disabled={pagina.length < TAMANHO_DA_PAGINA}
                onClick={() => setInicio((atual) => atual + TAMANHO_DA_PAGINA)}
              >
                {t('massa.lote.proxima')}
              </Botao>
            </div>
          </>
        )}
      </Resultado>
    </Secao>
  );
};

const Andamento = ({ lote }: { readonly lote: LoteDaMassa }) => {
  const { t } = useTranslation();
  const cancelar = useCaso(TOKENS_DA_MASSA.CancelarLote);
  const [aviso, setAviso] = useState<string | null>(null);
  const acao = useAcao((id: string) => cancelar.executar(id), {
    invalidar: [CHAVES.lote(lote.id), CHAVES.lotes],
    aoTerminar: () => setAviso(t('massa.lote.cancelamentoPedido')),
  });
  return (
    <Secao titulo={t('massa.lote.andamento')}>
      <progress
        aria-label={t('massa.lote.progresso')}
        className="mb-3 w-full"
        max={lote.quantidade}
        value={lote.processados}
      />
      <dl className="grid grid-cols-[max-content_1fr] gap-x-4 gap-y-1 text-sm">
        <dt className="font-medium">{t('massa.lote.situacao')}</dt>
        <dd>{t(`massa.lotes.situacao.${lote.situacao}`)}</dd>
        <dt className="font-medium">{t('massa.lote.processados')}</dt>
        <dd>{t('massa.lotes.progresso', { processados: lote.processados, quantidade: lote.quantidade })}</dd>
        <dt className="font-medium">{t('massa.lote.consentimentos')}</dt>
        <dd>{lote.consentimentos}</dd>
        <dt className="font-medium">{t('massa.lote.recursos')}</dt>
        <dd>{lote.recursos}</dd>
        <dt className="font-medium">{t('massa.lote.data')}</dt>
        <dd title={lote.data}>{dataHora(lote.data)}</dd>
        {lote.motivo !== null && (
          <>
            <dt className="font-medium">{t('massa.lote.motivo')}</dt>
            <dd>{lote.motivo}</dd>
          </>
        )}
      </dl>
      {aviso !== null && (
        <p role="status" className="mt-2 text-sm text-teal-800">
          {aviso}
        </p>
      )}
      <div className="mt-3 space-y-2">
        <ListaDeErros falha={acao.falha} />
        {emAndamento(lote) && (
          <Confirmacao
            gatilho={t('massa.lote.cancelar')}
            titulo={t('massa.lote.cancelarTitulo', { id: lote.id })}
            aoConfirmar={() => acao.executar(lote.id)}
            desabilitado={acao.ocupado}
          >
            {t('massa.lote.cancelarTexto')}
          </Confirmacao>
        )}
      </div>
    </Secao>
  );
};

/** Relê a cada 2 s só enquanto o lote está em andamento (US3, cenário 2). */
export const Lote = () => {
  const { t } = useTranslation();
  const { id = '' } = useParams();
  const obter = useCaso(TOKENS_DA_MASSA.Lote);
  const [rodando, setRodando] = useState(false);
  const consulta = useConsulta(CHAVES.lote(id), () => obter.executar(id), { intervaloMs: rodando ? 2000 : false });
  const dado = consulta.data;
  const rodandoAgora = dado !== undefined && dado.isRight() && emAndamento(dado.value);
  if (rodandoAgora !== rodando) setRodando(rodandoAgora);

  return (
    <>
      <Titulo
        ajuda="lote"
        acoes={
          <Link className={classeDoLink} to="/massa/lotes">
            {t('massa.lote.voltar')}
          </Link>
        }
      >
        {t('massa.lote.titulo', { id })}
      </Titulo>
      <Resultado consulta={consulta} seInexistente={t('massa.lote.inexistente', { id })}>
        {(lote) => (
          <>
            <Andamento lote={lote} />
            <ClientesDoLote id={lote.id} rodando={emAndamento(lote)} />
          </>
        )}
      </Resultado>
    </>
  );
};
