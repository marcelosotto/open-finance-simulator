import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DA_MASSA } from '@/application/massa/casos';
import type { PedidoDeLote } from '@/core/massa/massa';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Campo, Selecao } from '../comum/campo';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Tabela, Titulo } from '../comum/titulo';
import { dataHora } from '../comum/datas';
import { CHAVES, classeDoLink, numeroOpcional, separar } from './chaves';

interface Formulario {
  quantidade: string;
  distribuicaoNome: string;
  consentimentosPorCliente: string;
  historicoMeses: string;
  semente: string;
  permissoes: string;
}

const paraPedido = (dados: Formulario): PedidoDeLote => {
  const permissoes = separar(dados.permissoes);
  // Campo vazio fica undefined e sai do JSON: o servidor aplica o padrão (contrato da 003).
  return {
    quantidade: Number(dados.quantidade),
    distribuicaoNome: dados.distribuicaoNome === '' ? undefined : dados.distribuicaoNome,
    consentimentosPorCliente: numeroOpcional(dados.consentimentosPorCliente),
    historicoMeses: numeroOpcional(dados.historicoMeses),
    semente: numeroOpcional(dados.semente),
    permissoes: permissoes.length === 0 ? undefined : permissoes,
  };
};

const GerarLote = () => {
  const { t } = useTranslation();
  const navegar = useNavigate();
  const distribuicoes = useCaso(TOKENS_DA_MASSA.Distribuicoes);
  const gerar = useCaso(TOKENS_DA_MASSA.GerarLote);
  const consulta = useConsulta(CHAVES.distribuicoes, () => distribuicoes.executar());
  const acao = useAcao((dados: Formulario) => gerar.executar(paraPedido(dados)), {
    invalidar: [CHAVES.lotes],
    aoTerminar: (lote) => navegar(`/massa/lotes/${encodeURIComponent(lote.id)}`),
  });
  const { register, handleSubmit } = useForm<Formulario>({
    defaultValues: {
      quantidade: '10',
      distribuicaoNome: '',
      consentimentosPorCliente: '1',
      historicoMeses: '12',
      semente: '',
      permissoes: '',
    },
  });
  return (
    <Secao titulo={t('massa.lotes.gerar')}>
      <Resultado consulta={consulta}>
        {(salvas) => (
          <form
            onSubmit={handleSubmit((dados) => acao.executar(dados))}
            className="grid max-w-2xl gap-3 sm:grid-cols-2"
          >
            <Campo type="number" min={1} rotulo={t('massa.lotes.quantidade')} {...register('quantidade')} />
            <Selecao rotulo={t('massa.lotes.distribuicao')} {...register('distribuicaoNome')}>
              <option value="">{t('massa.lotes.distribuicaoPadrao')}</option>
              {salvas.map((salva) => (
                <option key={salva.nome} value={salva.nome}>
                  {salva.nome}
                </option>
              ))}
            </Selecao>
            <Campo
              type="number"
              min={1}
              rotulo={t('massa.lotes.consentimentosPorCliente')}
              {...register('consentimentosPorCliente')}
            />
            <Campo type="number" min={1} rotulo={t('massa.lotes.historicoMeses')} {...register('historicoMeses')} />
            <Campo type="number" rotulo={t('massa.lotes.semente')} {...register('semente')} />
            <Campo rotulo={t('massa.lotes.permissoes')} {...register('permissoes')} />
            <div className="sm:col-span-2">
              <ListaDeErros falha={acao.falha} />
            </div>
            <div>
              <Botao type="submit" disabled={acao.ocupado}>
                {t('massa.lotes.gerar')}
              </Botao>
            </div>
          </form>
        )}
      </Resultado>
    </Secao>
  );
};

export const Lotes = () => {
  const { t } = useTranslation();
  const listar = useCaso(TOKENS_DA_MASSA.Lotes);
  const consulta = useConsulta(CHAVES.lotes, () => listar.executar());
  return (
    <>
      <Titulo>{t('massa.lotes.titulo')}</Titulo>
      <Secao titulo={t('massa.lotes.lista')}>
        <Resultado consulta={consulta}>
          {(lotes) =>
            lotes.length === 0 ? (
              <p>{t('massa.lotes.vazio')}</p>
            ) : (
              <Tabela rotulo={t('massa.lotes.lista')}>
                <thead>
                  <tr>
                    <th>{t('massa.lotes.colunas.id')}</th>
                    <th>{t('massa.lotes.colunas.quantidade')}</th>
                    <th>{t('massa.lotes.colunas.situacao')}</th>
                    <th>{t('massa.lotes.colunas.andamento')}</th>
                    <th>{t('massa.lotes.colunas.data')}</th>
                  </tr>
                </thead>
                <tbody>
                  {lotes.map((lote) => (
                    <tr key={lote.id}>
                      <td>
                        <Link className={classeDoLink} to={`/massa/lotes/${encodeURIComponent(lote.id)}`}>
                          {lote.id}
                        </Link>
                      </td>
                      <td>{lote.quantidade}</td>
                      <td>{t(`massa.lotes.situacao.${lote.situacao}`)}</td>
                      <td>
                        {t('massa.lotes.progresso', { processados: lote.processados, quantidade: lote.quantidade })}
                      </td>
                      <td title={lote.data}>{dataHora(lote.data)}</td>
                    </tr>
                  ))}
                </tbody>
              </Tabela>
            )
          }
        </Resultado>
      </Secao>
      <GerarLote />
    </>
  );
};
