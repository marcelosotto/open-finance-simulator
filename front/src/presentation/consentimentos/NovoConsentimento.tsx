import { useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DOS_CONSENTIMENTOS } from '@/application/consentimentos/casos';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import {
  alternarGrupo,
  alternarOrigem,
  alternarPermissao,
  estadoDoGrupo,
  mostraOrigem,
  origemMarcada,
  origensFaltando,
  PERMISSAO_DE_RECURSOS,
  type OpcoesDoMenu,
  type Origem,
  type Receptora,
} from '@/core/consentimentos/consentimento';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Campo, Selecao } from '../comum/campo';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Titulo } from '../comum/titulo';
import { PASSOS, useMenu } from './menu';
import { CHAVES } from './chaves';
import { SeletorDeCliente } from '../massa/SeletorDeCliente';

const Navegacao = ({ podeAvancar, aoAvancar }: { readonly podeAvancar: boolean; readonly aoAvancar?: () => void }) => {
  const { t } = useTranslation();
  const { passo, voltar, avancar } = useMenu();
  return (
    <div className="mt-4 flex gap-2">
      {passo > 0 && (
        <Botao variante="secundario" onClick={voltar}>
          {t('comum.voltar')}
        </Botao>
      )}
      <Botao disabled={!podeAvancar} onClick={aoAvancar ?? avancar}>
        {t('comum.avancar')}
      </Botao>
    </div>
  );
};

/** Com uma receptora só, ela é usada direto; com mais de uma, Bruno escolhe (RN124, UI10). */
const EscolhaDaReceptora = ({ receptoras }: { readonly receptoras: readonly Receptora[] }) => {
  const { t } = useTranslation();
  const { rascunho, atualizar } = useMenu();
  const unica = receptoras.length === 1 ? receptoras[0] : undefined;
  useEffect(() => {
    if (unica !== undefined && rascunho.receptora !== unica.organisationId) {
      atualizar((r) => ({ ...r, receptora: unica.organisationId }));
    }
  }, [unica, rascunho.receptora, atualizar]);

  if (receptoras.length === 0) {
    return (
      <p role="alert" className="rounded border border-amber-300 bg-amber-50 p-3 text-sm text-amber-950">
        {t('consentimentos.menu.semReceptora')}{' '}
        <Link className="font-medium underline" to="/receptoras">
          {t('nav.receptoras')}
        </Link>
      </p>
    );
  }
  if (unica !== undefined) {
    return <p className="text-sm">{t('consentimentos.menu.receptoraUnica', { nome: unica.nome })}</p>;
  }
  return (
    <Selecao
      rotulo={t('consentimentos.menu.receptora')}
      value={rascunho.receptora}
      onChange={(e) => {
        const receptora = e.target.value;
        atualizar((r) => ({ ...r, receptora }));
      }}
    >
      <option value="">{t('consentimentos.menu.escolha')}</option>
      {receptoras.map((r) => (
        <option key={r.organisationId} value={r.organisationId}>
          {r.nome}
        </option>
      ))}
    </Selecao>
  );
};

/** Passo 1: banco, cliente da lista do banco (UI19) e receptora. */
const PassoBancoClienteEReceptora = () => {
  const { t } = useTranslation();
  const { rascunho, atualizar } = useMenu();
  const bancos = useCaso(TOKENS_DAS_INSTITUICOES.Listar);
  const receptoras = useCaso(TOKENS_DOS_CONSENTIMENTOS.Receptoras);
  const consultaDeBancos = useConsulta(['instituicoes'], () => bancos.executar());
  const consultaDeReceptoras = useConsulta(CHAVES.receptoras, () => receptoras.executar());
  return (
    <div className="grid max-w-xl gap-3">
      <Resultado consulta={consultaDeBancos}>
        {(lista) => {
          const ativos = lista.filter((b) => b.situacao === 'ativa');
          return (
            <>
              <Selecao
                rotulo={t('consentimentos.menu.banco')}
                value={rascunho.banco}
                onChange={(e) => {
                  const banco = e.target.value;
                  atualizar((r) => ({ ...r, banco, cliente: '', permissoes: [], invertidas: [] }));
                }}
              >
                <option value="">{t('consentimentos.menu.escolha')}</option>
                {ativos.map((b) => (
                  <option key={b.endereco} value={b.endereco}>
                    {b.nome}
                  </option>
                ))}
              </Selecao>
              <SeletorDeCliente
                banco={rascunho.banco}
                nomeDoBanco={ativos.find((b) => b.endereco === rascunho.banco)?.nome}
                rotulo={t('consentimentos.menu.cliente')}
                valor={rascunho.cliente}
                aoMudar={(cliente) => atualizar((r) => ({ ...r, cliente, permissoes: [], invertidas: [] }))}
              />
            </>
          );
        }}
      </Resultado>
      <Resultado consulta={consultaDeReceptoras}>{(lista) => <EscolhaDaReceptora receptoras={lista} />}</Resultado>
      <Navegacao podeAvancar={rascunho.banco !== '' && rascunho.cliente.trim() !== '' && rascunho.receptora !== ''} />
    </div>
  );
};

const SecaoDeOrigem = ({ opcoes, origem }: { readonly opcoes: OpcoesDoMenu; readonly origem: Origem }) => {
  const { t } = useTranslation();
  const { rascunho, atualizar } = useMenu();
  if (!mostraOrigem(rascunho, opcoes, origem)) return null;
  return (
    <fieldset className="rounded border p-3">
      <legend className="px-1 text-sm font-medium">{t(`consentimentos.menu.${origem}`)}</legend>
      {opcoes[origem].map((item) => (
        <label key={item.resourceId} className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={origemMarcada(rascunho, item)}
            onChange={() => atualizar((r) => alternarOrigem(r, item.resourceId))}
          />
          <span className="font-mono">{item.resourceId}</span>
        </label>
      ))}
    </fieldset>
  );
};

/** Grupos abríveis, com as permissões marcadas e desmarcáveis (Q6), e a seleção de origem (UI13). */
const PassoPermissoes = ({ opcoes }: { readonly opcoes: OpcoesDoMenu }) => {
  const { t } = useTranslation();
  const { rascunho, atualizar } = useMenu();
  if (opcoes.semProduto) {
    return (
      <>
        <p role="alert">{t('consentimentos.menu.semProduto')}</p>
        <Navegacao podeAvancar={false} />
      </>
    );
  }
  const faltando = origensFaltando(rascunho, opcoes);
  return (
    <div className="space-y-3">
      <p className="text-sm">{t('consentimentos.menu.tipo', { tipo: opcoes.tipo })}</p>
      <fieldset className="space-y-1">
        <legend className="text-sm font-medium">{t('consentimentos.menu.grupos')}</legend>
        {opcoes.grupos.map((grupo) => {
          const estado = estadoDoGrupo(rascunho, grupo);
          return (
            <details key={grupo.nome} className="rounded border px-3 py-1">
              <summary className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  aria-label={t('consentimentos.menu.grupo', { nome: grupo.nome })}
                  checked={estado === 'marcado'}
                  ref={(caixa) => {
                    if (caixa) caixa.indeterminate = estado === 'parcial';
                  }}
                  onChange={() => atualizar((r) => alternarGrupo(r, grupo, opcoes.grupos))}
                />
                {grupo.nome}
              </summary>
              <div
                role="group"
                aria-label={t('consentimentos.menu.verPermissoes', { nome: grupo.nome })}
                className="py-1 pl-6"
              >
                {grupo.permissoes.map((permissao) => (
                  <label key={permissao} className="flex items-center gap-2 font-mono text-xs">
                    <input
                      type="checkbox"
                      checked={rascunho.permissoes.includes(permissao)}
                      onChange={() => atualizar((r) => alternarPermissao(r, permissao))}
                    />
                    {permissao}
                  </label>
                ))}
              </div>
            </details>
          );
        })}
      </fieldset>
      <p className="text-xs text-slate-600">
        <span className="font-mono">{PERMISSAO_DE_RECURSOS}</span> — {t('consentimentos.menu.recursosSempre')}
      </p>
      <SecaoDeOrigem opcoes={opcoes} origem="contas" />
      <SecaoDeOrigem opcoes={opcoes} origem="cartoes" />
      {opcoes.agrupados.length > 0 && (
        <p className="text-sm text-slate-700">
          {t('consentimentos.menu.agrupados', { tipos: opcoes.agrupados.join(', ') })}
        </p>
      )}
      {faltando.length > 0 && (
        <p role="alert" className="text-sm text-red-800">
          {t('consentimentos.menu.origemFaltando', {
            origens: faltando.map((o) => t(`consentimentos.menu.${o}`)).join(', '),
          })}
        </p>
      )}
      <Navegacao podeAvancar={rascunho.permissoes.length > 0 && faltando.length === 0} />
    </div>
  );
};

const PassoPrazo = ({ opcoes }: { readonly opcoes: OpcoesDoMenu }) => {
  const { t } = useTranslation();
  const { rascunho, atualizar } = useMenu();
  return (
    <div className="grid max-w-md gap-3">
      <Campo
        rotulo={t('consentimentos.menu.prazo')}
        type="number"
        min={1}
        max={12}
        value={rascunho.prazoMeses}
        onChange={(e) => {
          const prazoMeses = e.target.value;
          atualizar((r) => ({ ...r, prazoMeses }));
        }}
      />
      {opcoes.tipo === 'PJ' && (
        <Campo
          rotulo={t('consentimentos.menu.alcadas')}
          type="number"
          min={1}
          max={10}
          value={rascunho.aprovacoesPendentes}
          onChange={(e) => {
            const aprovacoesPendentes = e.target.value;
            atualizar((r) => ({ ...r, aprovacoesPendentes }));
          }}
        />
      )}
      <Navegacao podeAvancar />
    </div>
  );
};

const PassoConfirmacao = ({ opcoes }: { readonly opcoes: OpcoesDoMenu }) => {
  const { t } = useTranslation();
  const navegar = useNavigate();
  const { rascunho, voltar, recomecar } = useMenu();
  const criar = useCaso(TOKENS_DOS_CONSENTIMENTOS.Criar);
  const acao = useAcao(() => criar.executar({ rascunho, opcoes }), {
    aoTerminar: (visao) => {
      recomecar();
      navegar(`/consentimentos/${encodeURIComponent(visao.consentId)}`);
    },
  });
  return (
    <div className="space-y-3">
      <dl className="grid grid-cols-[max-content_1fr] gap-x-4 gap-y-1 text-sm">
        <dt>{t('consentimentos.visao.cliente')}</dt>
        <dd>{rascunho.cliente}</dd>
        <dt>{t('consentimentos.visao.banco')}</dt>
        <dd>{rascunho.banco}</dd>
        <dt>{t('consentimentos.visao.receptora')}</dt>
        <dd>{opcoes.receptoras.find((r) => r.organisationId === rascunho.receptora)?.nome ?? rascunho.receptora}</dd>
        <dt>{t('consentimentos.visao.permissoes')}</dt>
        <dd className="font-mono text-xs">{[...rascunho.permissoes, PERMISSAO_DE_RECURSOS].join(', ')}</dd>
        <dt>{t('consentimentos.visao.expiraEm')}</dt>
        <dd>{rascunho.prazoMeses.trim() === '' ? t('consentimentos.menu.indeterminado') : rascunho.prazoMeses}</dd>
      </dl>
      <ListaDeErros falha={acao.falha} />
      <div className="flex gap-2">
        <Botao variante="secundario" onClick={voltar}>
          {t('comum.voltar')}
        </Botao>
        <Botao disabled={acao.ocupado} onClick={() => acao.executar(undefined)}>
          {t('consentimentos.menu.confirmar')}
        </Botao>
      </div>
    </div>
  );
};

/** Os passos que dependem das opções do menu: cliente e banco já escolhidos. */
const PassosComOpcoes = ({ passo }: { readonly passo: (typeof PASSOS)[number] }) => {
  const { t } = useTranslation();
  const { rascunho, voltar } = useMenu();
  const opcoes = useCaso(TOKENS_DOS_CONSENTIMENTOS.Opcoes);
  const consulta = useConsulta(CHAVES.opcoes(rascunho.cliente.trim(), rascunho.banco), () =>
    opcoes.executar({ cliente: rascunho.cliente, banco: rascunho.banco }),
  );
  return (
    <>
      <Resultado consulta={consulta} seInexistente={t('consentimentos.menu.opcoesInexistentes')}>
        {(valor) => {
          if (passo === 'permissoes') return <PassoPermissoes opcoes={valor} />;
          if (passo === 'prazo') return <PassoPrazo opcoes={valor} />;
          return <PassoConfirmacao opcoes={valor} />;
        }}
      </Resultado>
      {consulta.data?.isLeft() === true && (
        <Botao className="mt-2" variante="secundario" onClick={voltar}>
          {t('comum.voltar')}
        </Botao>
      )}
    </>
  );
};

/** UI10: banco, cliente e receptora; permissões e origem; prazo e alçadas; confirmação. */
export const NovoConsentimento = () => {
  const { t } = useTranslation();
  const { passo, recomecar } = useMenu();
  const nome = PASSOS[passo] ?? 'banco-e-cliente';
  return (
    <>
      <Titulo
        ajuda="novoConsentimento"
        acoes={
          <Botao variante="secundario" onClick={recomecar}>
            {t('consentimentos.menu.recomecar')}
          </Botao>
        }
      >
        {t('consentimentos.menu.titulo')}
      </Titulo>
      <ol aria-label={t('consentimentos.menu.titulo')} className="mb-4 flex flex-wrap gap-2 text-sm">
        {PASSOS.map((p, i) => (
          <li
            key={p}
            aria-current={i === passo ? 'step' : undefined}
            className={i === passo ? 'font-semibold text-teal-800' : 'text-slate-500'}
          >
            {i + 1}. {t(`consentimentos.menu.passos.${p}`)}
          </li>
        ))}
      </ol>
      <Secao
        titulo={t('consentimentos.menu.passoAtual', {
          numero: passo + 1,
          total: PASSOS.length,
          nome: t(`consentimentos.menu.passos.${nome}`),
        })}
      >
        {nome === 'banco-e-cliente' ? <PassoBancoClienteEReceptora /> : <PassosComOpcoes passo={nome} />}
      </Secao>
    </>
  );
};
