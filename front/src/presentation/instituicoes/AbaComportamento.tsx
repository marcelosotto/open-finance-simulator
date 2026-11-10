import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { CircleCheck, CircleX } from 'lucide-react';
import { CLASSES, type Instituicao, type LatenciaDaClasse } from '@/core/instituicoes/instituicao';
import { escreverPesos, lerPesos } from '@/core/instituicoes/pesos';
import type { Falha } from '@/core/comum/falha';
import { Campo } from '../comum/campo';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Tabela } from '../comum/titulo';
import { useSalvarBanco } from './salvar';

interface Formulario {
  latencia: Record<string, LatenciaDaClasse>;
  taxaDeErroPct: number;
  recursoIndisponivelPct: number;
  pesos: string;
  janelaDuracao: string;
  janelaIntervalo: string;
}

const numero = { valueAsNumber: true, required: true } as const;

/** UI04: SLA oficial de cada classe e se o configurado fica dentro ou fora (RN11). */
export const AbaComportamento = ({ banco }: { readonly banco: Instituicao }) => {
  const { t } = useTranslation();
  const salvar = useSalvarBanco(banco.endereco);
  const [falhaLocal, setFalhaLocal] = useState<Falha | null>(null);
  const c = banco.comportamento;
  const { register, handleSubmit } = useForm<Formulario>({
    defaultValues: {
      latencia: { ...c.latenciaPorClasse },
      taxaDeErroPct: c.taxaDeErroPct,
      recursoIndisponivelPct: c.recursoIndisponivelPct,
      pesos: escreverPesos(c.pesosPorStatus),
      janelaDuracao: c.janela ? String(c.janela.duracaoMin) : '',
      janelaIntervalo: c.janela ? String(c.janela.intervaloMin) : '',
    },
  });

  const enviar = (dados: Formulario) => {
    const pesos = lerPesos(dados.pesos);
    if (pesos.isLeft()) {
      setFalhaLocal(pesos.value);
      return;
    }
    setFalhaLocal(null);
    const temJanela = dados.janelaDuracao.trim() !== '' && dados.janelaIntervalo.trim() !== '';
    salvar.executar({
      comportamento: {
        latenciaPorClasse: dados.latencia,
        taxaDeErroPct: dados.taxaDeErroPct,
        recursoIndisponivelPct: dados.recursoIndisponivelPct,
        pesosPorStatus: pesos.value,
        janela: temJanela
          ? { duracaoMin: Number(dados.janelaDuracao), intervaloMin: Number(dados.janelaIntervalo) }
          : null,
      },
    });
  };

  return (
    <Secao titulo={t('instituicoes.abas.comportamento')}>
      <form onSubmit={handleSubmit(enviar)} className="space-y-4">
        <Tabela rotulo={t('instituicoes.abas.comportamento')}>
          <thead>
            <tr>
              <th>{t('instituicoes.comportamento.classe')}</th>
              <th>{t('instituicoes.comportamento.tipico')}</th>
              <th>{t('instituicoes.comportamento.variacao')}</th>
              <th>{t('instituicoes.comportamento.cauda')}</th>
              <th>{t('instituicoes.comportamento.sla')}</th>
              <th>{t('instituicoes.comportamento.p95')}</th>
            </tr>
          </thead>
          <tbody>
            {CLASSES.filter((classe) => c.latenciaPorClasse[classe] !== undefined).map((classe) => {
              const sla = banco.sla.find((s) => s.classe === classe);
              return (
                <tr key={classe}>
                  <td>{classe}</td>
                  <td>
                    <input
                      aria-label={`${t('instituicoes.comportamento.tipico')} ${classe}`}
                      type="number"
                      className="w-24 rounded border px-1"
                      {...register(`latencia.${classe}.tipicoMs`, numero)}
                    />
                  </td>
                  <td>
                    <input
                      aria-label={`${t('instituicoes.comportamento.variacao')} ${classe}`}
                      type="number"
                      className="w-24 rounded border px-1"
                      {...register(`latencia.${classe}.variacaoMs`, numero)}
                    />
                  </td>
                  <td>
                    <input
                      aria-label={`${t('instituicoes.comportamento.cauda')} ${classe}`}
                      type="number"
                      step="any"
                      className="w-20 rounded border px-1"
                      {...register(`latencia.${classe}.caudaLentaPct`, numero)}
                    />
                  </td>
                  <td>{sla?.slaMs ?? '—'}</td>
                  <td>
                    {sla === undefined ? (
                      '—'
                    ) : (
                      <span className={sla.dentroDoSla ? 'text-green-800' : 'text-red-800'}>
                        {sla.p95Ms}{' '}
                        {sla.dentroDoSla ? (
                          <CircleCheck aria-label={t('instituicoes.comportamento.dentro')} className="inline h-4 w-4" />
                        ) : (
                          <CircleX aria-label={t('instituicoes.comportamento.fora')} className="inline h-4 w-4" />
                        )}
                      </span>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </Tabela>
        <div className="grid max-w-2xl grid-cols-2 gap-3">
          <Campo
            rotulo={t('instituicoes.comportamento.taxaDeErro')}
            type="number"
            step="any"
            {...register('taxaDeErroPct', numero)}
          />
          <Campo
            rotulo={t('instituicoes.comportamento.recursoIndisponivel')}
            type="number"
            step="any"
            {...register('recursoIndisponivelPct', numero)}
          />
          <Campo className="col-span-2" rotulo={t('instituicoes.comportamento.pesos')} {...register('pesos')} />
          <Campo rotulo={t('instituicoes.comportamento.janelaDuracao')} type="number" {...register('janelaDuracao')} />
          <Campo
            rotulo={t('instituicoes.comportamento.janelaIntervalo')}
            type="number"
            {...register('janelaIntervalo')}
          />
        </div>
        <ListaDeErros falha={falhaLocal ?? salvar.falha} />
        {salvar.aviso !== null && <p role="status">{salvar.aviso}</p>}
        <Botao type="submit" disabled={salvar.ocupado}>
          {t('comum.salvar')}
        </Botao>
      </form>
    </Secao>
  );
};
