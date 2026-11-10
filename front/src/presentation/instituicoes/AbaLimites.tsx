import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { TriangleAlert } from 'lucide-react';
import { avisoDoCampo, type Instituicao, type Limites } from '@/core/instituicoes/instituicao';
import { Campo } from '../comum/campo';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao } from '../comum/titulo';
import { useSalvarBanco } from './salvar';

type CampoNumerico =
  'qca' | 'capacidadeTps' | 'pageSizeMaximo' | 'validadeDaChaveMin' | 'retryAfterSeg' | 'preparacaoDeRecursosSeg';
const NUMERICOS: readonly CampoNumerico[] = [
  'qca',
  'capacidadeTps',
  'pageSizeMaximo',
  'validadeDaChaveMin',
  'retryAfterSeg',
  'preparacaoDeRecursosSeg',
];
const TPM = ['mediaAlta', 'media', 'baixa'] as const;
const MENSAL = ['alta', 'saldosELimites', 'mediaAlta', 'media', 'baixa'] as const;

interface Formulario {
  numeros: Record<CampoNumerico, number>;
  tpm: { mediaAlta: number; media: number; baixa: number; alta: string };
  limiteMensal: Limites['limiteMensal'];
  limiteOperacionalLigado: boolean;
  retryAfterLigado: boolean;
}

const numero = { valueAsNumber: true, required: true } as const;

/** Aviso ao lado do campo, com o oficial, quando o configurado fica abaixo (UI05, RN26). */
const Aviso = ({ banco, campo }: { readonly banco: Instituicao; readonly campo: string }) => {
  const { t } = useTranslation();
  const aviso = avisoDoCampo(banco, campo);
  if (aviso === undefined) return null;
  return (
    <span className="inline-flex items-center gap-1 text-amber-800">
      <TriangleAlert className="h-3.5 w-3.5" aria-hidden />{' '}
      {t('instituicoes.limites.abaixo', { oficial: aviso.oficial })}
    </span>
  );
};

/** Aceita qualquer valor; o servidor devolve os avisos de RN26. */
export const AbaLimites = ({ banco }: { readonly banco: Instituicao }) => {
  const { t } = useTranslation();
  const salvar = useSalvarBanco(banco.endereco);
  const l = banco.limites;
  const { register, handleSubmit } = useForm<Formulario>({
    defaultValues: {
      numeros: Object.fromEntries(NUMERICOS.map((campo) => [campo, l[campo]])) as Record<CampoNumerico, number>,
      tpm: {
        mediaAlta: l.tpm.mediaAlta,
        media: l.tpm.media,
        baixa: l.tpm.baixa,
        alta: l.tpm.alta === null ? '' : String(l.tpm.alta),
      },
      limiteMensal: { ...l.limiteMensal },
      limiteOperacionalLigado: l.limiteOperacionalLigado,
      retryAfterLigado: l.retryAfterLigado,
    },
  });
  const enviar = (dados: Formulario) =>
    salvar.executar({
      limites: {
        ...dados.numeros,
        tpm: {
          mediaAlta: dados.tpm.mediaAlta,
          media: dados.tpm.media,
          baixa: dados.tpm.baixa,
          alta: dados.tpm.alta.trim() === '' ? null : Number(dados.tpm.alta),
        },
        limiteMensal: dados.limiteMensal,
        limiteOperacionalLigado: dados.limiteOperacionalLigado,
        retryAfterLigado: dados.retryAfterLigado,
      },
    });

  return (
    <Secao titulo={t('instituicoes.abas.limites')}>
      {banco.avisos.length > 0 && (
        <div role="alert" className="mb-4 rounded border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900">
          <p className="font-medium">{t('instituicoes.limites.avisos')}</p>
          <ul className="list-disc pl-6">
            {banco.avisos.map((aviso) => (
              <li key={aviso.campo}>
                {aviso.campo}: {aviso.configurado} ({t('instituicoes.limites.oficial', { oficial: aviso.oficial })})
              </li>
            ))}
          </ul>
        </div>
      )}
      <form onSubmit={handleSubmit(enviar)} className="space-y-4">
        <div className="grid grid-cols-2 gap-3 md:grid-cols-3">
          {NUMERICOS.map((campo) => (
            <Campo
              key={campo}
              rotulo={t(`instituicoes.limites.${campo}`)}
              type="number"
              ajuda={<Aviso banco={banco} campo={campo} />}
              {...register(`numeros.${campo}`, numero)}
            />
          ))}
          {TPM.map((classe) => (
            <Campo
              key={classe}
              rotulo={t('instituicoes.limites.tpm', { classe })}
              type="number"
              ajuda={<Aviso banco={banco} campo={`tpm.${classe}`} />}
              {...register(`tpm.${classe}`, numero)}
            />
          ))}
          <Campo
            rotulo={t('instituicoes.limites.tpmAlta')}
            type="number"
            ajuda={<Aviso banco={banco} campo="tpm.alta" />}
            {...register('tpm.alta')}
          />
          {MENSAL.map((classe) => (
            <Campo
              key={classe}
              rotulo={t('instituicoes.limites.limiteMensal', { classe })}
              type="number"
              ajuda={<Aviso banco={banco} campo={`limiteMensal.${classe}`} />}
              {...register(`limiteMensal.${classe}`, numero)}
            />
          ))}
        </div>
        <div className="flex gap-6 text-sm">
          <label className="flex items-center gap-2">
            <input type="checkbox" {...register('limiteOperacionalLigado')} />{' '}
            {t('instituicoes.limites.limiteOperacionalLigado')}
          </label>
          <label className="flex items-center gap-2">
            <input type="checkbox" {...register('retryAfterLigado')} /> {t('instituicoes.limites.retryAfterLigado')}
          </label>
        </div>
        <ListaDeErros falha={salvar.falha} />
        {salvar.aviso !== null && <p role="status">{salvar.aviso}</p>}
        <Botao type="submit" disabled={salvar.ocupado}>
          {t('comum.salvar')}
        </Botao>
      </form>
    </Secao>
  );
};
