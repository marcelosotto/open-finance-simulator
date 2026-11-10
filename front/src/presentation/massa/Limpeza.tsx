import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { TOKENS_DA_MASSA } from '@/application/massa/casos';
import {
  ALVOS_DA_LIMPEZA,
  CONTAGENS_DA_LIMPEZA,
  alvoPedeId,
  type AlvoDaLimpeza,
  type ResultadoDaLimpeza,
} from '@/core/massa/massa';
import { useCaso } from '../comum/casos';
import { useAcao } from '../comum/consultas';
import { Campo, Selecao } from '../comum/campo';
import { Botao } from '../comum/botao';
import { Confirmacao } from '../comum/confirmacao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Titulo } from '../comum/titulo';
import { CHAVES } from './chaves';

interface Pedido {
  readonly alvo: AlvoDaLimpeza;
  readonly id: string;
}

const Contagens = ({ titulo, resultado }: { readonly titulo: string; readonly resultado: ResultadoDaLimpeza }) => {
  const { t } = useTranslation();
  return (
    <section aria-label={titulo} className="mt-3 rounded border border-slate-200 p-3">
      <h3 className="mb-2 text-sm font-semibold">{titulo}</h3>
      <ul className="space-y-1 text-sm">
        {CONTAGENS_DA_LIMPEZA.map((campo) => (
          <li key={campo}>
            {t('massa.limpeza.contagem', { rotulo: t(`massa.limpeza.contagens.${campo}`), valor: resultado[campo] })}
          </li>
        ))}
      </ul>
    </section>
  );
};

/** Primeiro a prévia; só com ela na tela o botão de apagar aparece (US3, cenário 5). */
export const Limpeza = () => {
  const { t } = useTranslation();
  const prever = useCaso(TOKENS_DA_MASSA.PreverLimpeza);
  const confirmar = useCaso(TOKENS_DA_MASSA.ConfirmarLimpeza);
  const [pedido, setPedido] = useState<Pedido>({ alvo: 'lote', id: '' });
  const [previa, setPrevia] = useState<ResultadoDaLimpeza | null>(null);
  const [feita, setFeita] = useState<ResultadoDaLimpeza | null>(null);
  const acaoDePrevia = useAcao((p: Pedido) => prever.executar(p), {
    aoTerminar: (resultado) => {
      setFeita(null);
      setPrevia(resultado);
    },
  });
  const acaoDeConfirmar = useAcao((p: Pedido) => confirmar.executar(p), {
    invalidar: [['massa'], CHAVES.lotes],
    aoTerminar: (resultado) => {
      setPrevia(null);
      setFeita(resultado);
    },
  });

  // Mudou o pedido, a prévia antiga não vale mais.
  const mudar = (novo: Pedido) => {
    setPedido(novo);
    setPrevia(null);
    acaoDePrevia.limpar();
    acaoDeConfirmar.limpar();
  };

  return (
    <>
      <Titulo ajuda="limpeza">{t('massa.limpeza.titulo')}</Titulo>
      <Secao titulo={t('massa.limpeza.titulo')}>
        <p className="mb-3 text-sm text-slate-600">{t('massa.limpeza.explicacao')}</p>
        <form
          className="grid max-w-xl gap-3"
          onSubmit={(evento) => {
            evento.preventDefault();
            acaoDePrevia.executar(pedido);
          }}
        >
          <Selecao
            rotulo={t('massa.limpeza.alvo')}
            value={pedido.alvo}
            onChange={(e) => {
              const alvo = ALVOS_DA_LIMPEZA.find((a) => a === e.target.value) ?? 'lote';
              mudar({ ...pedido, alvo });
            }}
          >
            {ALVOS_DA_LIMPEZA.map((alvo) => (
              <option key={alvo} value={alvo}>
                {t(`massa.limpeza.alvos.${alvo}`)}
              </option>
            ))}
          </Selecao>
          {alvoPedeId(pedido.alvo) && (
            <Campo
              rotulo={t('massa.limpeza.id')}
              value={pedido.id}
              onChange={(e) => mudar({ ...pedido, id: e.target.value })}
            />
          )}
          <ListaDeErros falha={acaoDePrevia.falha} />
          <div>
            <Botao type="submit" variante="secundario" disabled={acaoDePrevia.ocupado}>
              {t('massa.limpeza.previa')}
            </Botao>
          </div>
        </form>
        {previa !== null && (
          <>
            <Contagens titulo={t('massa.limpeza.oQueSera')} resultado={previa} />
            <div className="mt-3 space-y-2">
              <ListaDeErros falha={acaoDeConfirmar.falha} />
              <Confirmacao
                gatilho={t('massa.limpeza.confirmar')}
                titulo={t('massa.limpeza.confirmarTitulo')}
                aoConfirmar={() => acaoDeConfirmar.executar(pedido)}
                desabilitado={acaoDeConfirmar.ocupado}
              >
                {t('massa.limpeza.confirmarTexto')}
              </Confirmacao>
            </div>
          </>
        )}
        {feita !== null && (
          <>
            <p role="status" className="mt-3 text-sm text-teal-800">
              {t('massa.limpeza.feita')}
            </p>
            <Contagens titulo={t('massa.limpeza.oQueFoi')} resultado={feita} />
          </>
        )}
      </Secao>
    </>
  );
};
