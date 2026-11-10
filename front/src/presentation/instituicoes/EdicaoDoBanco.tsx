import * as Tabs from '@radix-ui/react-tabs';
import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import type { Instituicao } from '@/core/instituicoes/instituicao';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Botao } from '../comum/botao';
import { Confirmacao } from '../comum/confirmacao';
import { ListaDeErros } from '../comum/estado';
import { Titulo } from '../comum/titulo';
import { CHAVES } from './chaves';
import { AbaGeral } from './AbaGeral';
import { AbaComportamento } from './AbaComportamento';
import { AbaLimites } from './AbaLimites';
import { AbaAnomalias } from './AbaAnomalias';
import { AbaRelogio } from './AbaRelogio';
import { AbaHistorico } from './AbaHistorico';

const ABAS = ['geral', 'comportamento', 'limites', 'anomalias', 'relogio', 'historico'] as const;

const Acoes = ({ banco }: { readonly banco: Instituicao }) => {
  const { t } = useTranslation();
  const navegar = useNavigate();
  const [aviso, setAviso] = useState<string | null>(null);
  const apagar = useCaso(TOKENS_DAS_INSTITUICOES.Apagar);
  const reativar = useCaso(TOKENS_DAS_INSTITUICOES.Reativar);
  const remocao = useAcao((endereco: string) => apagar.executar(endereco), {
    invalidar: [CHAVES.lista, CHAVES.detalhe(banco.endereco)],
    aoTerminar: (resultado) =>
      resultado === 'apagada' ? navegar('/bancos') : setAviso(t('instituicoes.remover.inativada')),
  });
  const reativacao = useAcao((endereco: string) => reativar.executar(endereco), {
    invalidar: [CHAVES.lista, CHAVES.detalhe(banco.endereco)],
    aoTerminar: () => setAviso(null),
  });
  return (
    <div className="flex flex-col items-end gap-2">
      <div className="flex gap-2">
        {banco.situacao === 'inativa' && (
          <Botao variante="secundario" onClick={() => reativacao.executar(banco.endereco)}>
            {t('instituicoes.reativar')}
          </Botao>
        )}
        <Confirmacao
          gatilho={t('instituicoes.remover.botao')}
          titulo={t('instituicoes.remover.titulo', { nome: banco.nome })}
          aoConfirmar={() => remocao.executar(banco.endereco)}
        >
          {t('instituicoes.remover.texto')}
        </Confirmacao>
      </div>
      {aviso !== null && <p role="status">{aviso}</p>}
      <ListaDeErros falha={remocao.falha ?? reativacao.falha} />
    </div>
  );
};

/** Edição do banco, com as abas de UI04, UI05, anomalias (RN113), relógio (UC28) e histórico (RN71). */
export const EdicaoDoBanco = () => {
  const { t } = useTranslation();
  const { endereco = '' } = useParams();
  const obter = useCaso(TOKENS_DAS_INSTITUICOES.Obter);
  const consulta = useConsulta(CHAVES.detalhe(endereco), () => obter.executar(endereco));
  return (
    <Resultado consulta={consulta}>
      {(banco) => (
        <>
          <Titulo acoes={<Acoes banco={banco} />}>
            {banco.nome}{' '}
            <span className="text-sm font-normal text-slate-500">({t(`instituicoes.situacao.${banco.situacao}`)})</span>
          </Titulo>
          <Tabs.Root defaultValue="geral">
            <Tabs.List aria-label={banco.nome} className="mb-4 flex flex-wrap gap-1 border-b">
              {ABAS.map((aba) => (
                <Tabs.Trigger
                  key={aba}
                  value={aba}
                  className="rounded-t px-3 py-1.5 text-sm data-[state=active]:border-b-2 data-[state=active]:border-teal-700 data-[state=active]:font-semibold"
                >
                  {t(`instituicoes.abas.${aba}`)}
                </Tabs.Trigger>
              ))}
            </Tabs.List>
            <Tabs.Content value="geral">
              <AbaGeral banco={banco} />
            </Tabs.Content>
            <Tabs.Content value="comportamento">
              <AbaComportamento banco={banco} />
            </Tabs.Content>
            <Tabs.Content value="limites">
              <AbaLimites banco={banco} />
            </Tabs.Content>
            <Tabs.Content value="anomalias">
              <AbaAnomalias endereco={banco.endereco} />
            </Tabs.Content>
            <Tabs.Content value="relogio">
              <AbaRelogio banco={banco} />
            </Tabs.Content>
            <Tabs.Content value="historico">
              <AbaHistorico endereco={banco.endereco} />
            </Tabs.Content>
          </Tabs.Root>
        </>
      )}
    </Resultado>
  );
};
