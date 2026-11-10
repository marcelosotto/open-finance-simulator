import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import { relogioFixado, type Instituicao, type PedidoDeRelogio } from '@/core/instituicoes/instituicao';
import { useCaso } from '../comum/casos';
import { useAcao } from '../comum/consultas';
import { Campo } from '../comum/campo';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao } from '../comum/titulo';
import { dataHora } from '../comum/datas';
import { CHAVES } from './chaves';

/** Fixar, acelerar ou voltar ao relógio real (UC28, RN73). */
export const AbaRelogio = ({ banco }: { readonly banco: Instituicao }) => {
  const { t } = useTranslation();
  const mudar = useCaso(TOKENS_DAS_INSTITUICOES.MudarRelogio);
  const [fixadoEm, setFixadoEm] = useState('');
  const [fator, setFator] = useState('60');
  const acao = useAcao((relogio: PedidoDeRelogio | null) => mudar.executar({ endereco: banco.endereco, relogio }), {
    invalidar: [CHAVES.detalhe(banco.endereco), CHAVES.historico(banco.endereco)],
  });
  const relogio = banco.relogio;
  let situacao = t('instituicoes.relogio.real');
  if (relogio !== null) {
    situacao = relogioFixado(relogio)
      ? t('instituicoes.relogio.fixado', { hora: dataHora(relogio.baseSimulada) })
      : t('instituicoes.relogio.acelerado', { fator: relogio.fator, hora: dataHora(relogio.baseSimulada) });
  }
  return (
    <Secao titulo={t('instituicoes.abas.relogio')}>
      <p role="status" className="mb-3">
        {situacao}
      </p>
      <div className="grid max-w-2xl gap-3 md:grid-cols-2">
        <div className="flex items-end gap-2">
          <Campo
            className="flex-1"
            rotulo={t('instituicoes.relogio.fixarEm')}
            type="datetime-local"
            step={1}
            value={fixadoEm}
            onChange={(e) => setFixadoEm(e.target.value)}
          />
          <Botao
            disabled={fixadoEm.trim() === '' || acao.ocupado}
            onClick={() => acao.executar({ fixadoEm: new Date(fixadoEm).toISOString() })}
          >
            {t('instituicoes.relogio.fixar')}
          </Botao>
        </div>
        <div className="flex items-end gap-2">
          <Campo
            className="flex-1"
            rotulo={t('instituicoes.relogio.fator')}
            type="number"
            value={fator}
            onChange={(e) => setFator(e.target.value)}
          />
          <Botao disabled={fator.trim() === '' || acao.ocupado} onClick={() => acao.executar({ fator: Number(fator) })}>
            {t('instituicoes.relogio.acelerar')}
          </Botao>
        </div>
      </div>
      <div className="mt-3">
        <Botao variante="secundario" disabled={relogio === null || acao.ocupado} onClick={() => acao.executar(null)}>
          {t('instituicoes.relogio.voltar')}
        </Botao>
      </div>
      <div className="mt-3">
        <ListaDeErros falha={acao.falha} />
      </div>
    </Secao>
  );
};
