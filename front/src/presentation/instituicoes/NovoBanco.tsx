import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Campo, Selecao } from '../comum/campo';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Titulo } from '../comum/titulo';
import { CHAVES } from './chaves';

interface Formulario {
  nome: string;
  perfil: string;
}

/** UI03: só nome e perfil; depois abre a edição preenchida pelo servidor (Q5). */
export const NovoBanco = () => {
  const { t } = useTranslation();
  const navegar = useNavigate();
  const perfis = useCaso(TOKENS_DAS_INSTITUICOES.Perfis);
  const criar = useCaso(TOKENS_DAS_INSTITUICOES.Criar);
  const consulta = useConsulta(CHAVES.perfis, () => perfis.executar());
  const acao = useAcao((dados: Formulario) => criar.executar(dados), {
    invalidar: [CHAVES.lista],
    aoTerminar: (banco) => navegar(`/bancos/${banco.endereco}`),
  });
  const { register, handleSubmit, formState } = useForm<Formulario>({ defaultValues: { nome: '', perfil: 'estavel' } });

  return (
    <>
      <Titulo ajuda="novoBanco">{t('instituicoes.criar.titulo')}</Titulo>
      <Secao titulo={t('instituicoes.criar.titulo')}>
        <p className="mb-3 text-sm text-slate-600">{t('instituicoes.criar.explicacao')}</p>
        <Resultado consulta={consulta}>
          {(nomes) => (
            <form onSubmit={handleSubmit((dados) => acao.executar(dados))} className="grid max-w-md gap-3">
              <Campo
                rotulo={t('instituicoes.criar.nome')}
                erro={formState.errors.nome ? t('comum.obrigatorio') : undefined}
                {...register('nome', { validate: (valor) => valor.trim().length > 0 })}
              />
              <Selecao rotulo={t('instituicoes.criar.perfil')} {...register('perfil')}>
                {nomes.map((nome) => (
                  <option key={nome} value={nome}>
                    {nome}
                  </option>
                ))}
              </Selecao>
              <ListaDeErros falha={acao.falha} />
              <div>
                <Botao type="submit" disabled={acao.ocupado}>
                  {t('comum.criar')}
                </Botao>
              </div>
            </form>
          )}
        </Resultado>
      </Secao>
    </>
  );
};
