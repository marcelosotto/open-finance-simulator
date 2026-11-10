import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Secao, Titulo } from '../comum/titulo';
import { classeDoLink } from './chaves';

const ITENS = [
  ['/massa/perfis', 'massa.menu.perfis'],
  ['/massa/distribuicoes', 'massa.menu.distribuicoes'],
  ['/massa/lotes', 'massa.menu.lotes'],
  ['/massa/clientes', 'massa.menu.clientes'],
  ['/massa/limpeza', 'massa.menu.limpeza'],
] as const;

export const IndiceDaMassa = () => {
  const { t } = useTranslation();
  return (
    <>
      <Titulo ajuda="massa">{t('massa.titulo')}</Titulo>
      <Secao titulo={t('massa.titulo')}>
        <p className="mb-3 text-sm text-slate-600">{t('massa.explicacao')}</p>
        <ul className="list-disc space-y-1 pl-6">
          {ITENS.map(([caminho, chave]) => (
            <li key={caminho}>
              <Link className={classeDoLink} to={caminho}>
                {t(chave)}
              </Link>
            </li>
          ))}
        </ul>
      </Secao>
    </>
  );
};
