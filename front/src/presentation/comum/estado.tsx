import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Loader2 } from 'lucide-react';
import type { Falha } from '@/core/comum/falha';
import { Botao } from './botao';

export const Carregando = () => {
  const { t } = useTranslation();
  return (
    <p role="status" className="flex items-center gap-2 text-slate-500">
      <Loader2 className="h-4 w-4 animate-spin" aria-hidden /> {t('comum.carregando')}
    </p>
  );
};

/** Todos os códigos da recusa, em português quando conhecidos, e o próprio código quando não (FR-006). */
export const ListaDeErros = ({ falha }: { readonly falha: Falha | null }) => {
  const { t } = useTranslation();
  if (falha === null) return null;
  return (
    <div role="alert" className="rounded border border-red-300 bg-red-50 p-3 text-sm text-red-800">
      <p className="flex items-center gap-2 font-medium">
        <AlertTriangle className="h-4 w-4" aria-hidden /> {t(`falhas.${falha.tipo}`)}
      </p>
      <ul className="mt-1 list-disc pl-6">
        {falha.erros.map((erro) => (
          <li key={erro}>{t(`erros.${erro}`, { defaultValue: erro })}</li>
        ))}
      </ul>
    </div>
  );
};

interface PropsDoErro {
  readonly falha: Falha;
  readonly aoTentar: () => void;
  /** Texto da tela dizendo o que faltou e como resolver (UI19). */
  readonly explicacao?: ReactNode;
}

export const ErroComNovaTentativa = ({ falha, aoTentar, explicacao }: PropsDoErro) => {
  const { t } = useTranslation();
  return (
    <div className="space-y-2">
      {explicacao === undefined ? (
        <ListaDeErros falha={falha} />
      ) : (
        <div role="alert" className="rounded border border-amber-300 bg-amber-50 p-3 text-sm text-amber-950">
          {explicacao}
        </div>
      )}
      <Botao variante="secundario" onClick={aoTentar}>
        {t('comum.tentarDeNovo')}
      </Botao>
    </div>
  );
};
