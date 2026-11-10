import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DA_MASSA } from '@/application/massa/casos';
import { rotuloDoCliente } from '@/core/massa/massa';
import { useCaso } from '../comum/casos';
import { Resultado, useConsulta } from '../comum/consultas';
import { Campo, Selecao } from '../comum/campo';
import { CHAVES } from './chaves';

interface PropsDoSeletor {
  /** Endereço do banco; vazio, lista todos os clientes (só quando `opcional`). */
  readonly banco: string;
  /** Nome do banco para a mensagem de banco sem clientes. */
  readonly nomeDoBanco?: string;
  readonly valor: string;
  readonly aoMudar: (documento: string) => void;
  readonly rotulo: string;
  /** Cliente não obrigatório (filtros de cenário): a primeira opção é "qualquer cliente". */
  readonly opcional?: boolean;
}

const LIMITE = 1000;

const SemClientes = ({ nomeDoBanco }: { readonly nomeDoBanco: string | null }) => {
  const { t } = useTranslation();
  return (
    <p role="alert" className="rounded border border-amber-300 bg-amber-50 p-3 text-sm text-amber-950">
      {nomeDoBanco === null
        ? t('seletorDeCliente.semClientesNaMassa')
        : t('seletorDeCliente.semClientes', { banco: nomeDoBanco })}{' '}
      <Link className="font-medium underline" to="/massa/lotes">
        {t('seletorDeCliente.gerarMassa')}
      </Link>{' '}
      {t('seletorDeCliente.ou')}{' '}
      <Link className="underline" to="/massa/clientes">
        {t('seletorDeCliente.criarAvulso')}
      </Link>
      .
    </p>
  );
};

/** Escolha de cliente pela lista do banco, com nome, documento e PF ou PJ, em vez de digitar o documento (UI19). */
export const SeletorDeCliente = ({ banco, nomeDoBanco, valor, aoMudar, rotulo, opcional = false }: PropsDoSeletor) => {
  const { t } = useTranslation();
  const listar = useCaso(TOKENS_DA_MASSA.ClientesDoBanco);
  const ativa = opcional || banco !== '';
  const consulta = useConsulta(
    CHAVES.clientesDoBanco(banco),
    () => listar.executar({ banco: banco === '' ? null : banco, pagina: { inicio: 0, quantidade: LIMITE } }),
    { ativa },
  );

  if (!ativa)
    return (
      <Selecao rotulo={rotulo} disabled value="" onChange={() => undefined}>
        <option value="">{t('seletorDeCliente.escolhaOBanco')}</option>
      </Selecao>
    );

  return (
    <div className="space-y-2">
      <Resultado consulta={consulta} seInexistente={t('seletorDeCliente.bancoInexistente', { banco })}>
        {(pagina) => {
          const naLista = pagina.clientes.some((cliente) => cliente.documento === valor);
          return (
            <>
              {pagina.clientes.length === 0 ? (
                // Num filtro opcional, lista vazia não é problema: fica só o campo para digitar.
                !opcional && <SemClientes nomeDoBanco={banco === '' ? null : (nomeDoBanco ?? banco)} />
              ) : (
                <Selecao
                  rotulo={rotulo}
                  value={naLista ? valor : ''}
                  onChange={(evento) => aoMudar(evento.target.value)}
                >
                  <option value="">{opcional ? t('seletorDeCliente.qualquer') : t('seletorDeCliente.escolha')}</option>
                  {pagina.clientes.map((cliente) => (
                    <option key={cliente.documento} value={cliente.documento}>
                      {rotuloDoCliente(cliente)}
                    </option>
                  ))}
                </Selecao>
              )}
              {pagina.temMais && <p className="text-xs text-slate-600">{t('seletorDeCliente.temMais')}</p>}
              <details open={(valor !== '' && !naLista) || (opcional && pagina.clientes.length === 0)}>
                <summary className="cursor-pointer text-xs text-slate-600">{t('seletorDeCliente.digitar')}</summary>
                <Campo
                  className="mt-1"
                  rotulo={t('seletorDeCliente.documento')}
                  value={naLista ? '' : valor}
                  onChange={(evento) => aoMudar(evento.target.value.trim())}
                />
              </details>
            </>
          );
        }}
      </Resultado>
    </div>
  );
};
