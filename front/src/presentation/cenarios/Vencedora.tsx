import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DOS_CENARIOS } from '@/application/cenarios/casos';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import { alcance } from '@/core/cenarios/alcance';
import type { RegraDeCenario, Vencedoras } from '@/core/cenarios/regra';
import { useCaso } from '../comum/casos';
import { useConsulta } from '../comum/consultas';
import { Campo, Selecao } from '../comum/campo';
import { SeletorDeCliente } from '../massa/SeletorDeCliente';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Tabela } from '../comum/titulo';
import { dataHora } from '../comum/datas';
import { CHAVES } from './chaves';
import { useDescricaoDaRegra } from './descricao';
import { useSugestoesDeEndpoint } from './endpoints';

interface Formulario {
  instituicao: string;
  endpoint: string;
  cliente: string;
  recurso: string;
}

const GRUPOS = ['comportamento', 'estado', 'defeito'] as const;
const vazio: Formulario = { instituicao: '', endpoint: '', cliente: '', recurso: '' };

const Vencedora = ({ regra }: { readonly regra: RegraDeCenario | null }) => {
  const { t } = useTranslation();
  const descricao = useDescricaoDaRegra();
  if (regra === null) return <>{t('cenarios.vencedora.nenhuma')}</>;
  return (
    <>
      <Link className="text-teal-800 underline" to={`/cenarios/regras/${encodeURIComponent(regra.id)}`}>
        {descricao.rotulo(regra)}
      </Link>{' '}
      <span className="text-slate-600">
        ({t(`cenarios.alcances.${alcance(regra)}`)}) {descricao.efeito(regra.efeito)}
      </span>
    </>
  );
};

/**
 * "Qual regra vale para esta chamada?": a resposta vem da administração, que usa a mesma ordem do pipeline.
 * A tela não decide quem vence (UI06, Q9).
 */
export const PainelDaVencedora = () => {
  const { t } = useTranslation();
  const listarBancos = useCaso(TOKENS_DAS_INSTITUICOES.Listar);
  const vencedora = useCaso(TOKENS_DOS_CENARIOS.Vencedora);
  const bancos = useConsulta(CHAVES.instituicoes, () => listarBancos.executar());
  const endpoints = useSugestoesDeEndpoint();
  // A pergunta fica guardada e a resposta é uma consulta sob a chave das regras: ligar, desligar, apagar ou criar
  // uma regra invalida essa chave e a resposta é refeita, sem ficar velha (revisão da 010).
  const [pedido, setPedido] = useState<Formulario | null>(null);
  const consulta = useConsulta([...CHAVES.regras, 'vencedora', pedido], () => vencedora.executar(pedido ?? vazio), {
    ativa: pedido !== null,
  });
  const resultado: Vencedoras | null = consulta.data?.isRight() === true ? consulta.data.value : null;
  const falha = consulta.data?.isLeft() === true ? consulta.data.value : null;
  const { register, handleSubmit, watch, setValue } = useForm<Formulario>({
    defaultValues: { instituicao: '', endpoint: '', cliente: '', recurso: '' },
  });
  const lista = bancos.data?.isRight() ? bancos.data.value : [];

  return (
    <Secao titulo={t('cenarios.vencedora.titulo')}>
      <p className="mb-3 text-sm text-slate-600">{t('cenarios.vencedora.explicacao')}</p>
      <form
        onSubmit={handleSubmit((dados) => setPedido({ ...dados }))}
        className="grid gap-3 md:grid-cols-5 md:items-end"
      >
        <Selecao rotulo={t('cenarios.vencedora.banco')} {...register('instituicao')}>
          <option value="">{t('cenarios.escolha')}</option>
          {lista.map((banco) => (
            <option key={banco.endereco} value={banco.endereco}>
              {banco.nome}
            </option>
          ))}
        </Selecao>
        <Campo rotulo={t('cenarios.vencedora.endpoint')} list={endpoints.lista} {...register('endpoint')} />
        <SeletorDeCliente
          opcional
          banco={watch('instituicao')}
          nomeDoBanco={lista.find((b) => b.endereco === watch('instituicao'))?.nome}
          rotulo={t('cenarios.vencedora.cliente')}
          valor={watch('cliente')}
          aoMudar={(documento) => setValue('cliente', documento)}
        />
        <Campo rotulo={t('cenarios.vencedora.recurso')} {...register('recurso')} />
        <div>
          <Botao type="submit" disabled={consulta.isFetching}>
            {t('cenarios.vencedora.consultar')}
          </Botao>
        </div>
        {endpoints.sugestoes}
      </form>
      <div className="mt-3 space-y-2">
        <ListaDeErros falha={bancos.data?.isLeft() ? bancos.data.value : null} />
        <ListaDeErros falha={falha} />
        {resultado !== null && (
          <>
            <p>{t('cenarios.vencedora.instante', { hora: dataHora(resultado.instante) })}</p>
            <Tabela rotulo={t('cenarios.vencedora.resultado')}>
              <tbody>
                {GRUPOS.map((grupo) => (
                  <tr key={grupo}>
                    <th scope="row">{t(`cenarios.vencedora.grupos.${grupo}`)}</th>
                    <td>
                      <Vencedora regra={resultado[grupo]} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </Tabela>
          </>
        )}
      </div>
    </Secao>
  );
};
