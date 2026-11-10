import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DA_MASSA } from '@/application/massa/casos';
import { PERFIS_PRONTOS, type PedidoDeCliente, type TipoDePessoa } from '@/core/massa/massa';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Campo, Selecao } from '../comum/campo';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Titulo } from '../comum/titulo';
import { CHAVES, numeroOpcional, separar } from './chaves';

interface Formulario {
  tipo: TipoDePessoa;
  perfil: string;
  bancos: string;
  nome: string;
  semente: string;
}

const paraPedido = (dados: Formulario): PedidoDeCliente => ({
  tipo: dados.tipo,
  perfil: dados.perfil,
  bancos: separar(dados.bancos),
  nome: dados.nome.trim() === '' ? undefined : dados.nome.trim(),
  semente: numeroOpcional(dados.semente),
});

const caminhoDoCliente = (documento: string) => `/massa/clientes/${encodeURIComponent(documento)}`;

const BuscaDeCliente = () => {
  const { t } = useTranslation();
  const navegar = useNavigate();
  const [documento, setDocumento] = useState('');
  return (
    <Secao titulo={t('massa.clientes.buscar')}>
      <form
        className="flex max-w-xl items-end gap-2"
        onSubmit={(evento) => {
          evento.preventDefault();
          if (documento.trim() !== '') navegar(caminhoDoCliente(documento.trim()));
        }}
      >
        <Campo
          className="flex-1"
          rotulo={t('massa.clientes.documento')}
          value={documento}
          onChange={(e) => setDocumento(e.target.value)}
        />
        <Botao type="submit" disabled={documento.trim() === ''}>
          {t('massa.clientes.abrir')}
        </Botao>
      </form>
    </Secao>
  );
};

const NovoCliente = () => {
  const { t } = useTranslation();
  const navegar = useNavigate();
  const perfis = useCaso(TOKENS_DA_MASSA.Perfis);
  const criar = useCaso(TOKENS_DA_MASSA.CriarCliente);
  const consulta = useConsulta(CHAVES.perfis, () => perfis.executar());
  const acao = useAcao((dados: Formulario) => criar.executar(paraPedido(dados)), {
    aoTerminar: (massa) => navegar(caminhoDoCliente(massa.cliente.documento)),
  });
  const { register, handleSubmit } = useForm<Formulario>({
    defaultValues: { tipo: 'PF', perfil: PERFIS_PRONTOS[0], bancos: '', nome: '', semente: '' },
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
            <Campo
              className="sm:col-span-2"
              rotulo={t('massa.clientes.bancos')}
              placeholder="banco-estavel, banco-lento"
              {...register('bancos')}
            />
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
      <Titulo>{t('massa.clientes.titulo')}</Titulo>
      <BuscaDeCliente />
      <NovoCliente />
    </>
  );
};
