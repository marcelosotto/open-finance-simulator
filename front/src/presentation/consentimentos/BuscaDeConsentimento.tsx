import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Campo } from '../comum/campo';
import { Botao } from '../comum/botao';
import { Secao, Titulo } from '../comum/titulo';

/** Sem lista na administração: busca por consentId; a lista do cliente fica na tela de Massa (Q7). */
export const BuscaDeConsentimento = () => {
  const { t } = useTranslation();
  const navegar = useNavigate();
  const [id, setId] = useState('');
  return (
    <>
      <Titulo>{t('consentimentos.busca.titulo')}</Titulo>
      <Secao titulo={t('consentimentos.busca.titulo')}>
        <form
          className="flex max-w-xl items-end gap-2"
          onSubmit={(evento) => {
            evento.preventDefault();
            if (id.trim() !== '') navegar(`/consentimentos/${encodeURIComponent(id.trim())}`);
          }}
        >
          <Campo
            className="flex-1"
            rotulo={t('consentimentos.busca.id')}
            value={id}
            onChange={(e) => setId(e.target.value)}
            placeholder="urn:hml:p-c0x0"
          />
          <Botao type="submit" disabled={id.trim() === ''}>
            {t('consentimentos.busca.abrir')}
          </Botao>
        </form>
        <p className="mt-3 text-sm text-slate-600">
          {t('consentimentos.busca.explicacao')}{' '}
          <Link className="text-teal-800 underline" to="/massa/clientes">
            {t('nav.massa')}
          </Link>
        </p>
      </Secao>
    </>
  );
};
