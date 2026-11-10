import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Info } from 'lucide-react';
import type { TelaComAjuda } from './textos';

/** Duas ou três linhas no topo: para que serve e o que precisa antes, com link para Primeiros passos (UI17). */
export const Ajuda = ({ tela }: { readonly tela: TelaComAjuda }) => {
  const { t } = useTranslation();
  const precisa = t(`ajuda.telas.${tela}.precisa`);
  return (
    <aside
      aria-label={t('ajuda.primeirosPassos')}
      className="mb-4 flex gap-2 rounded border border-sky-200 bg-sky-50 p-3 text-sm text-sky-950"
    >
      <Info className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
      <div>
        <p>{t(`ajuda.telas.${tela}.para`)}</p>
        <p className="mt-1">
          {precisa === '' ? t('ajuda.nadaAntes') : t('ajuda.precisa', { coisas: precisa })}{' '}
          {tela !== 'primeirosPassos' && (
            <Link className="font-medium underline" to="/primeiros-passos">
              {t('ajuda.primeirosPassos')}
            </Link>
          )}
        </p>
      </div>
    </aside>
  );
};
