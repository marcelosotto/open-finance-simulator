import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { CircleCheck, Circle } from 'lucide-react';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import { TOKENS_DA_MASSA } from '@/application/massa/casos';
import { TOKENS_DOS_CONSENTIMENTOS } from '@/application/consentimentos/casos';
import { useCaso } from '../comum/casos';
import { useConsulta } from '../comum/consultas';
import { Secao, Tabela, Titulo } from '../comum/titulo';
import type { TelaComAjuda } from '../ajuda/textos';

type Situacao = 'feito' | 'aFazer' | 'opcional';

interface PropsDoPasso {
  readonly numero: number;
  readonly titulo: string;
  readonly situacao: Situacao;
  readonly para: string;
  readonly children: ReactNode;
}

const Passo = ({ numero, titulo, situacao, para, children }: PropsDoPasso) => {
  const { t } = useTranslation();
  return (
    <li className="flex gap-3 rounded border border-slate-200 bg-white p-3">
      {situacao === 'feito' ? (
        <CircleCheck className="mt-0.5 h-5 w-5 shrink-0 text-green-700" aria-hidden />
      ) : (
        <Circle className="mt-0.5 h-5 w-5 shrink-0 text-slate-400" aria-hidden />
      )}
      <div className="space-y-1">
        <h2 className="font-semibold">
          {numero}. {titulo}{' '}
          <span className="ml-1 rounded bg-slate-100 px-2 py-0.5 text-xs font-normal text-slate-700">
            {t(`inicio.${situacao}`)}
          </span>
        </h2>
        <div className="text-sm text-slate-700">{children}</div>
        <Link className="text-sm font-medium text-teal-800 underline" to={para}>
          {t('inicio.abrir')}
        </Link>
      </div>
    </li>
  );
};

// Telas da tabela "tela → para que serve", com a ajuda de cada uma (UI17).
const TELAS: readonly (readonly [string, string, TelaComAjuda])[] = [
  ['/bancos', 'nav.bancos', 'bancos'],
  ['/massa/lotes', 'massa.lotes.titulo', 'lotes'],
  ['/massa/clientes', 'massa.clientes.titulo', 'clientes'],
  ['/consentimentos/novo', 'nav.novoConsentimento', 'novoConsentimento'],
  ['/consentimentos', 'nav.consentimentos', 'consentimentos'],
  ['/receptoras', 'nav.receptoras', 'receptoras'],
  ['/massa', 'nav.massa', 'massa'],
  ['/cenarios', 'nav.cenarios', 'cenarios'],
];

/** UI18: os passos em ordem, com o que já existe marcado, e a tabela "tela → para que serve". */
export const PrimeirosPassos = () => {
  const { t } = useTranslation();
  const bancos = useCaso(TOKENS_DAS_INSTITUICOES.Listar);
  const resumo = useCaso(TOKENS_DA_MASSA.Resumo);
  const receptoras = useCaso(TOKENS_DOS_CONSENTIMENTOS.Receptoras);
  const consultaDeBancos = useConsulta(['instituicoes'], () => bancos.executar());
  const consultaDoResumo = useConsulta(['massa', 'resumo'], () => resumo.executar());
  const consultaDeReceptoras = useConsulta(['receptoras'], () => receptoras.executar());

  const ativos =
    consultaDeBancos.data?.isRight() === true ? consultaDeBancos.data.value.filter((b) => b.situacao === 'ativa') : [];
  const massa = consultaDoResumo.data?.isRight() === true ? consultaDoResumo.data.value : null;
  const receptora = consultaDeReceptoras.data?.isRight() === true ? consultaDeReceptoras.data.value[0] : undefined;
  const banco = ativos[0]?.endereco ?? 'banco-estavel';
  const api = `${window.location.protocol}//${window.location.hostname}:8080`;
  const marcar = (feito: boolean): Situacao => (feito ? 'feito' : 'aFazer');

  return (
    <>
      <Titulo ajuda="primeirosPassos">{t('inicio.titulo')}</Titulo>
      <ol aria-label={t('inicio.titulo')} className="mb-6 space-y-3">
        <Passo
          numero={1}
          titulo={t('inicio.passos.banco.titulo')}
          situacao={marcar(ativos.length > 0)}
          para="/bancos/novo"
        >
          {t('inicio.passos.banco.texto', { n: ativos.length })}
        </Passo>
        <Passo
          numero={2}
          titulo={t('inicio.passos.clientes.titulo')}
          situacao={marcar((massa?.clientes ?? 0) > 0)}
          para="/massa/lotes"
        >
          {t('inicio.passos.clientes.texto', { n: massa?.clientes ?? 0 })}
        </Passo>
        <Passo
          numero={3}
          titulo={t('inicio.passos.consentimento.titulo')}
          situacao={marcar((massa?.consentimentos ?? 0) > 0)}
          para="/consentimentos/novo"
        >
          {t('inicio.passos.consentimento.texto', { n: massa?.consentimentos ?? 0 })}
        </Passo>
        <Passo numero={4} titulo={t('inicio.passos.acompanhar.titulo')} situacao="opcional" para="/consentimentos">
          {t('inicio.passos.acompanhar.texto')}
        </Passo>
        <Passo numero={5} titulo={t('inicio.passos.api.titulo')} situacao="opcional" para="/receptoras">
          <p>{t('inicio.passos.api.texto')}</p>
          <p className="mt-1">
            {receptora === undefined
              ? t('inicio.passos.api.semReceptora')
              : t('inicio.passos.api.receptora', {
                  nome: receptora.nome,
                  usuario: receptora.usuario,
                  senha: receptora.senha,
                })}
          </p>
          <p className="mt-2 text-xs font-medium">{t('inicio.passos.api.exemplo')}</p>
          <pre
            aria-label={t('inicio.passos.api.exemplo')}
            className="mt-1 overflow-x-auto rounded bg-slate-900 p-2 text-xs text-slate-100"
          >
            {`curl -X POST ${api}/${banco}/token \\\n  -d grant_type=client_credentials \\\n  -d client_id=${receptora?.usuario ?? '<usuario>'} \\\n  -d client_secret=${receptora?.senha ?? '<senha>'} \\\n  -d scope=consents`}
          </pre>
        </Passo>
      </ol>
      <Secao titulo={t('inicio.telas.titulo')}>
        <Tabela rotulo={t('inicio.telas.titulo')}>
          <thead>
            <tr>
              <th>{t('inicio.telas.tela')}</th>
              <th>{t('inicio.telas.serve')}</th>
            </tr>
          </thead>
          <tbody>
            {TELAS.map(([para, rotulo, ajuda]) => (
              <tr key={para}>
                <td>
                  <Link className="text-teal-800 underline" to={para}>
                    {t(rotulo)}
                  </Link>
                </td>
                <td>{t(`ajuda.telas.${ajuda}.para`)}</td>
              </tr>
            ))}
          </tbody>
        </Tabela>
      </Secao>
    </>
  );
};
