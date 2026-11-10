import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { TOKENS_DOS_CONSENTIMENTOS } from '@/application/consentimentos/casos';
import type { ReceptoraCriada } from '@/core/consentimentos/consentimento';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Campo } from '../comum/campo';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Tabela, Titulo } from '../comum/titulo';
import { CHAVES } from './chaves';

/** O menu de consentimento exige uma receptora (Q1). */
export const Receptoras = () => {
  const { t } = useTranslation();
  const listar = useCaso(TOKENS_DOS_CONSENTIMENTOS.Receptoras);
  const criar = useCaso(TOKENS_DOS_CONSENTIMENTOS.CriarReceptora);
  const consulta = useConsulta(CHAVES.receptoras, () => listar.executar());
  const [nome, setNome] = useState('');
  const [criada, setCriada] = useState<ReceptoraCriada | null>(null);
  const acao = useAcao((valor: string) => criar.executar(valor), {
    invalidar: [CHAVES.receptoras],
    aoTerminar: (receptora) => {
      setCriada(receptora);
      setNome('');
    },
  });
  return (
    <>
      <Titulo ajuda="receptoras">{t('consentimentos.receptoras.titulo')}</Titulo>
      <Secao titulo={t('consentimentos.receptoras.criar')}>
        <form
          className="flex max-w-xl items-end gap-2"
          onSubmit={(evento) => {
            evento.preventDefault();
            acao.executar(nome);
          }}
        >
          <Campo
            className="flex-1"
            rotulo={t('consentimentos.receptoras.nome')}
            value={nome}
            onChange={(e) => setNome(e.target.value)}
          />
          <Botao type="submit" disabled={acao.ocupado}>
            {t('consentimentos.receptoras.criar')}
          </Botao>
        </form>
        <div className="mt-2">
          <ListaDeErros falha={acao.falha} />
        </div>
        {criada !== null && (
          <p role="status" className="mt-2 text-sm">
            {t('consentimentos.receptoras.credenciais', {
              nome: criada.nome,
              usuario: criada.usuario,
              senha: criada.senha,
            })}
          </p>
        )}
      </Secao>
      <Resultado consulta={consulta}>
        {(lista) =>
          lista.length === 0 ? (
            <p>{t('consentimentos.receptoras.vazio')}</p>
          ) : (
            <Tabela rotulo={t('consentimentos.receptoras.titulo')}>
              <thead>
                <tr>
                  <th>{t('consentimentos.receptoras.nome')}</th>
                  <th>organisationId</th>
                  <th>{t('consentimentos.receptoras.usuario')}</th>
                  <th>{t('consentimentos.receptoras.senha')}</th>
                </tr>
              </thead>
              <tbody>
                {lista.map((r) => (
                  <tr key={r.organisationId}>
                    <td>{r.nome}</td>
                    <td className="font-mono text-xs">{r.organisationId}</td>
                    <td className="font-mono text-xs">{r.usuario ?? '—'}</td>
                    <td className="font-mono text-xs">{r.senha ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </Tabela>
          )
        }
      </Resultado>
    </>
  );
};
