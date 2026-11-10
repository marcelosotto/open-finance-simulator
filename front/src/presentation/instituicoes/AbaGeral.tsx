import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import type { Instituicao, Publico } from '@/core/instituicoes/instituicao';
import { Campo, Selecao } from '../comum/campo';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao } from '../comum/titulo';
import { useSalvarBanco } from './salvar';

interface Formulario {
  nome: string;
  marca: string;
  publico: Publico;
  produtos: string;
}

export const AbaGeral = ({ banco }: { readonly banco: Instituicao }) => {
  const { t } = useTranslation();
  const salvar = useSalvarBanco(banco.endereco);
  const { register, handleSubmit } = useForm<Formulario>({
    defaultValues: {
      nome: banco.nome,
      marca: banco.marca,
      publico: banco.publico,
      produtos: banco.produtos.join(', '),
    },
  });
  const enviar = (dados: Formulario) =>
    salvar.executar({
      nome: dados.nome.trim(),
      marca: dados.marca.trim(),
      publico: dados.publico,
      produtos: dados.produtos
        .split(',')
        .map((p) => p.trim())
        .filter((p) => p.length > 0),
    });
  return (
    <Secao titulo={t('instituicoes.abas.geral')}>
      <form onSubmit={handleSubmit(enviar)} className="grid max-w-2xl gap-3">
        <p className="text-sm text-slate-600">
          {t('instituicoes.geral.organisationId')}: <span className="font-mono">{banco.organisationId}</span>
          {banco.pronta && <> · {t('instituicoes.geral.pronta')}</>}
        </p>
        <Campo rotulo={t('instituicoes.geral.nome')} {...register('nome')} />
        <Campo rotulo={t('instituicoes.geral.marca')} {...register('marca')} />
        <Selecao rotulo={t('instituicoes.geral.publico')} {...register('publico')}>
          <option value="ambos">ambos</option>
          <option value="PF">PF</option>
          <option value="PJ">PJ</option>
        </Selecao>
        <Campo rotulo={t('instituicoes.geral.produtos')} {...register('produtos')} />
        <ListaDeErros falha={salvar.falha} />
        {salvar.aviso !== null && <p role="status">{salvar.aviso}</p>}
        <div>
          <Botao type="submit" disabled={salvar.ocupado}>
            {t('comum.salvar')}
          </Botao>
        </div>
      </form>
    </Secao>
  );
};
