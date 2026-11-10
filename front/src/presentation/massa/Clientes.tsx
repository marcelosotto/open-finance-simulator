import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DA_MASSA } from '@/application/massa/casos';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import { PERFIS_PRONTOS, type PedidoDeCliente, type TipoDePessoa } from '@/core/massa/massa';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Campo, Selecao } from '../comum/campo';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Titulo } from '../comum/titulo';
import { CHAVES, numeroOpcional } from './chaves';
import { SeletorDeCliente } from './SeletorDeCliente';

interface Formulario {
  tipo: TipoDePessoa;
  perfil: string;
  bancos: string[];
  nome: string;
  semente: string;
}

const paraPedido = (dados: Formulario): PedidoDeCliente => ({
  tipo: dados.tipo,
  perfil: dados.perfil,
  bancos: dados.bancos,
  nome: dados.nome.trim() === '' ? undefined : dados.nome.trim(),
  semente: numeroOpcional(dados.semente),
});

const caminhoDoCliente = (documento: string) => `/massa/clientes/${encodeURIComponent(documento)}`;

/** Abre um cliente escolhendo banco e cliente na lista (UI19), sem digitar o documento. */
const BuscaDeCliente = () => {
  const { t } = useTranslation();
  const navegar = useNavigate();
  const listar = useCaso(TOKENS_DAS_INSTITUICOES.Listar);
  const bancos = useConsulta(['instituicoes'], () => listar.executar());
  const [banco, setBanco] = useState('');
  const [documento, setDocumento] = useState('');
  return (
    <Secao titulo={t('massa.clientes.buscar')}>
      <Resultado consulta={bancos}>
        {(lista) => (
          <form
            className="grid max-w-xl gap-3"
            onSubmit={(evento) => {
              evento.preventDefault();
              if (documento.trim() !== '') navegar(caminhoDoCliente(documento.trim()));
            }}
          >
            <Selecao
              rotulo={t('massa.clientes.banco')}
              value={banco}
              onChange={(e) => {
                setBanco(e.target.value);
                setDocumento('');
              }}
            >
              <option value="">{t('massa.clientes.todosOsBancos')}</option>
              {lista.map((b) => (
                <option key={b.endereco} value={b.endereco}>
                  {b.nome}
                </option>
              ))}
            </Selecao>
            <SeletorDeCliente
              opcional
              banco={banco}
              nomeDoBanco={lista.find((b) => b.endereco === banco)?.nome}
              rotulo={t('massa.clientes.cliente')}
              valor={documento}
              aoMudar={setDocumento}
            />
            <div>
              <Botao type="submit" disabled={documento.trim() === ''}>
                {t('massa.clientes.abrir')}
              </Botao>
            </div>
          </form>
        )}
      </Resultado>
    </Secao>
  );
};

const NovoCliente = () => {
  const { t } = useTranslation();
  const navegar = useNavigate();
  const perfis = useCaso(TOKENS_DA_MASSA.Perfis);
  const criar = useCaso(TOKENS_DA_MASSA.CriarCliente);
  const consulta = useConsulta(CHAVES.perfis, () => perfis.executar());
  const listarBancos = useCaso(TOKENS_DAS_INSTITUICOES.Listar);
  const bancos = useConsulta(['instituicoes'], () => listarBancos.executar());
  const ativos = bancos.data?.isRight() === true ? bancos.data.value.filter((b) => b.situacao === 'ativa') : [];
  const acao = useAcao((dados: Formulario) => criar.executar(paraPedido(dados)), {
    aoTerminar: (massa) => navegar(caminhoDoCliente(massa.cliente.documento)),
  });
  const { register, handleSubmit } = useForm<Formulario>({
    defaultValues: { tipo: 'PF', perfil: PERFIS_PRONTOS[0], bancos: [], nome: '', semente: '' },
  });
  return (
    <Secao titulo={t('massa.clientes.criar')}>
      <Resultado consulta={consulta}>
        {(lista) => (
          <form
            onSubmit={handleSubmit((dados) => acao.executar(dados))}
            className="grid max-w-2xl gap-3 sm:grid-cols-2"
          >
            <Selecao rotulo={t('massa.clientes.tipo')} {...register('tipo')}>
              <option value="PF">PF</option>
              <option value="PJ">PJ</option>
            </Selecao>
            <Selecao rotulo={t('massa.clientes.perfil')} {...register('perfil')}>
              {lista.map((perfil) => (
                <option key={perfil.nome} value={perfil.nome}>
                  {perfil.nome}
                </option>
              ))}
            </Selecao>
            <Selecao
              className="sm:col-span-2"
              multiple
              size={Math.min(Math.max(ativos.length, 3), 8)}
              rotulo={t('massa.clientes.bancos')}
              {...register('bancos')}
            >
              {ativos.map((b) => (
                <option key={b.endereco} value={b.endereco}>
                  {b.nome}
                </option>
              ))}
            </Selecao>
            <Campo rotulo={t('massa.clientes.nome')} {...register('nome')} />
            <Campo type="number" rotulo={t('massa.clientes.semente')} {...register('semente')} />
            <div className="sm:col-span-2">
              <ListaDeErros falha={acao.falha} />
            </div>
            <div>
              <Botao type="submit" disabled={acao.ocupado}>
                {t('comum.criar')}
              </Botao>
            </div>
          </form>
        )}
      </Resultado>
    </Secao>
  );
};

export const Clientes = () => {
  const { t } = useTranslation();
  return (
    <>
      <Titulo ajuda="clientes">{t('massa.clientes.titulo')}</Titulo>
      <BuscaDeCliente />
      <NovoCliente />
    </>
  );
};
