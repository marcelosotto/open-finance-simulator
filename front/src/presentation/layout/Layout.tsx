import { NavLink, Outlet } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { FlaskConical } from 'lucide-react';
import { TOKENS_DO_AMBIENTE } from '@/application/ambiente/obter-ambiente';
import type { CorDaMarca } from '@/core/ambiente/ambiente';
import { useCaso } from '../comum/casos';
import { useConsulta } from '../comum/consultas';
import { cn } from '../comum/cn';
import { useAutor } from './autor';

const CORES: Readonly<Record<CorDaMarca, string>> = {
  dev: 'bg-sky-600 text-white',
  hml: 'bg-amber-400 text-amber-950',
  stage: 'bg-violet-600 text-white',
  neutra: 'bg-slate-600 text-white',
};

const LINKS = [
  ['/primeiros-passos', 'nav.primeirosPassos'],
  ['/bancos', 'nav.bancos'],
  ['/consentimentos/novo', 'nav.novoConsentimento'],
  ['/consentimentos', 'nav.consentimentos'],
  ['/receptoras', 'nav.receptoras'],
  ['/massa', 'nav.massa'],
  ['/cenarios', 'nav.cenarios'],
] as const;

/** Marca do ambiente em destaque; vazia ou ainda não lida, nada aparece (UI01, RN72). */
export const MarcaDoAmbiente = () => {
  const { t } = useTranslation();
  const obter = useCaso(TOKENS_DO_AMBIENTE.ObterAmbiente);
  const consulta = useConsulta(['ambiente'], () => obter.executar());
  if (consulta.data?.isLeft() === true) {
    // Sem a marca, quem usa não sabe de qual ambiente vêm os dados: um aviso discreto, sem travar a tela.
    return (
      <span role="status" className="text-xs text-amber-200" title={consulta.data.value.erros.join(', ')}>
        {t('layout.semAmbiente')}
      </span>
    );
  }
  const ambiente = consulta.data?.isRight() === true ? consulta.data.value : null;
  if (ambiente === null || !ambiente.temMarca) return null;
  return (
    <span
      aria-label={t('layout.marca', { marca: ambiente.marca })}
      className={cn('rounded px-3 py-1 text-sm font-bold uppercase tracking-wide', CORES[ambiente.cor])}
    >
      {ambiente.marca}
    </span>
  );
};

const CampoDoAutor = () => {
  const { t } = useTranslation();
  const { nome, definir } = useAutor();
  return (
    <label className="flex items-center gap-2 text-sm">
      {t('layout.seuNome')}
      <input
        value={nome}
        onChange={(evento) => definir(evento.target.value)}
        placeholder={t('layout.autorPadrao')}
        className="w-36 rounded border border-slate-500 bg-slate-700 px-2 py-0.5 text-white placeholder:text-slate-300"
      />
    </label>
  );
};

export const Layout = () => {
  const { t } = useTranslation();
  return (
    <div className="min-h-screen">
      <p role="note" className="flex items-center justify-center gap-2 bg-teal-800 py-1 text-xs font-medium text-white">
        <FlaskConical className="h-3.5 w-3.5" aria-hidden /> {t('layout.faixa')}
      </p>
      <header className="flex flex-wrap items-center gap-4 bg-slate-800 px-4 py-2 text-white">
        <span className="font-semibold">{t('layout.titulo')}</span>
        <MarcaDoAmbiente />
        <nav aria-label={t('layout.navegacao')} className="flex flex-wrap gap-3 text-sm">
          {LINKS.map(([para, rotulo]) => (
            <NavLink
              key={para}
              to={para}
              end
              className={({ isActive }) => cn('hover:underline', isActive && 'underline')}
            >
              {t(rotulo)}
            </NavLink>
          ))}
        </nav>
        <div className="ml-auto">
          <CampoDoAutor />
        </div>
      </header>
      <main className="mx-auto max-w-6xl p-4">
        <Outlet />
      </main>
    </div>
  );
};
