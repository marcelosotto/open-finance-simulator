import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

export const NaoEncontrada = () => {
  const { t } = useTranslation();
  return (
    <div>
      <h1 className="text-xl font-semibold">{t('falhas.inexistente')}</h1>
      <Link className="text-teal-800 underline" to="/bancos">
        {t('nav.bancos')}
      </Link>
    </div>
  );
};
