import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { TOKENS_DA_MASSA } from '@/application/massa/casos';
import {
  podeSalvar,
  somaDeBancos,
  somaDePerfis,
  type Distribuicao,
  type DistribuicaoNomeada,
  type Porcentagens,
} from '@/core/massa/massa';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Campo } from '../comum/campo';
import { Botao } from '../comum/botao';
import { Confirmacao } from '../comum/confirmacao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Tabela, Titulo } from '../comum/titulo';
import { CHAVES } from './chaves';

type Grupo = 'bancos' | 'perfis' | 'produtos';

interface Linha {
  readonly chave: string;
  readonly pct: string;
}

type Linhas = Readonly<Record<Grupo, readonly Linha[]>>;

const GRUPOS: readonly Grupo[] = ['bancos', 'perfis', 'produtos'];

const paraLinhas = (porcentagens: Porcentagens): Linha[] =>
  Object.entries(porcentagens).map(([chave, pct]) => ({ chave, pct: String(pct) }));

// Campo vazio vira NaN: fica fora da faixa e bloqueia salvar.
const paraPorcentagens = (linhas: readonly Linha[]): Record<string, number> =>
  Object.fromEntries(linhas.map((linha) => [linha.chave.trim(), linha.pct.trim() === '' ? NaN : Number(linha.pct)]));

const comUma = (linhas: Linha[]): Linha[] => (linhas.length === 0 ? [{ chave: '', pct: '' }] : linhas);

const mostrarSoma = (soma: number): string => (Number.isFinite(soma) ? String(soma) : '—');

const NOVA: DistribuicaoNomeada = {
  nome: '',
  distribuicao: { bancos: {}, perfis: { 'cliente-comum': 100 }, produtos: {}, pjPct: 0 },
};

const GrupoDePorcentagens = ({
  grupo,
  linhas,
  mudar,
}: {
  readonly grupo: Grupo;
  readonly linhas: readonly Linha[];
  readonly mudar: (linhas: readonly Linha[]) => void;
}) => {
  const { t } = useTranslation();
  const titulo = t(`massa.distribuicoes.grupos.${grupo}.titulo`);
  const trocar = (indice: number, linha: Linha) => mudar(linhas.map((atual, i) => (i === indice ? linha : atual)));
  return (
    <fieldset className="rounded border border-slate-200 p-3">
      <legend className="px-1 text-sm font-semibold">{titulo}</legend>
      <div className="space-y-2">
        {linhas.map((linha, indice) => {
          const n = indice + 1;
          return (
            <div key={indice} className="flex items-end gap-2">
              <Campo
                className="flex-1"
                rotulo={t(`massa.distribuicoes.grupos.${grupo}.chave`, { n })}
                value={linha.chave}
                onChange={(e) => trocar(indice, { ...linha, chave: e.target.value })}
              />
              <Campo
                className="w-32"
                type="number"
                step="any"
                rotulo={t(`massa.distribuicoes.grupos.${grupo}.pct`, { n })}
                value={linha.pct}
                onChange={(e) => trocar(indice, { ...linha, pct: e.target.value })}
              />
              <Botao
                variante="secundario"
                aria-label={t('massa.distribuicoes.remover', { n, grupo: titulo })}
                onClick={() => mudar(linhas.filter((_, i) => i !== indice))}
              >
                ×
              </Botao>
            </div>
          );
        })}
      </div>
      <Botao className="mt-2" variante="secundario" onClick={() => mudar([...linhas, { chave: '', pct: '' }])}>
        {t('massa.distribuicoes.adicionar', { grupo: titulo })}
      </Botao>
    </fieldset>
  );
};

/** Mostra as somas e só deixa salvar com bancos e perfis em 100% (UI07, RN56). */
const EditorDeDistribuicao = ({
  inicial,
  aoSalvar,
}: {
  readonly inicial: DistribuicaoNomeada;
  readonly aoSalvar: () => void;
}) => {
  const { t } = useTranslation();
  const salvar = useCaso(TOKENS_DA_MASSA.SalvarDistribuicao);
  const [nome, setNome] = useState(inicial.nome);
  const [linhas, setLinhas] = useState<Linhas>({
    bancos: comUma(paraLinhas(inicial.distribuicao.bancos)),
    perfis: comUma(paraLinhas(inicial.distribuicao.perfis)),
    produtos: paraLinhas(inicial.distribuicao.produtos),
  });
  const [pjPct, setPjPct] = useState(String(inicial.distribuicao.pjPct));
  const acao = useAcao((pedido: { nome: string; distribuicao: Distribuicao }) => salvar.executar(pedido), {
    invalidar: [CHAVES.distribuicoes],
    aoTerminar: aoSalvar,
  });

  const distribuicao: Distribuicao = {
    bancos: paraPorcentagens(linhas.bancos),
    perfis: paraPorcentagens(linhas.perfis),
    produtos: paraPorcentagens(linhas.produtos),
    pjPct: pjPct.trim() === '' ? NaN : Number(pjPct),
  };
  const valida = podeSalvar(distribuicao);

  return (
    <form
      className="grid max-w-3xl gap-3"
      onSubmit={(evento) => {
        evento.preventDefault();
        acao.executar({ nome, distribuicao });
      }}
    >
      <Campo
        rotulo={t('massa.distribuicoes.nome')}
        value={nome}
        readOnly={inicial.nome !== ''}
        onChange={(e) => setNome(e.target.value)}
      />
      {GRUPOS.map((grupo) => (
        <GrupoDePorcentagens
          key={grupo}
          grupo={grupo}
          linhas={linhas[grupo]}
          mudar={(novas) => setLinhas((atuais) => ({ ...atuais, [grupo]: novas }))}
        />
      ))}
      <Campo
        className="w-40"
        type="number"
        step="any"
        rotulo={t('massa.distribuicoes.pjPct')}
        value={pjPct}
        onChange={(e) => setPjPct(e.target.value)}
      />
      <div aria-live="polite" className="text-sm">
        <p>{t('massa.distribuicoes.somaDeBancos', { soma: mostrarSoma(somaDeBancos(distribuicao)) })}</p>
        <p>{t('massa.distribuicoes.somaDePerfis', { soma: mostrarSoma(somaDePerfis(distribuicao)) })}</p>
        <p className="text-slate-600">{t('massa.distribuicoes.semSoma')}</p>
        {!valida && (
          <p id="distribuicao-fora-de-100" className="font-medium text-red-700">
            {t('massa.distribuicoes.foraDe100')}
          </p>
        )}
      </div>
      <ListaDeErros falha={acao.falha} />
      <div>
        <Botao
          type="submit"
          disabled={!valida || acao.ocupado}
          aria-describedby={valida ? undefined : 'distribuicao-fora-de-100'}
        >
          {t('comum.salvar')}
        </Botao>
      </div>
    </form>
  );
};

const resumir = (porcentagens: Porcentagens): string =>
  Object.entries(porcentagens)
    .map(([chave, pct]) => `${chave} ${pct}%`)
    .join(', ');

export const Distribuicoes = () => {
  const { t } = useTranslation();
  const listar = useCaso(TOKENS_DA_MASSA.Distribuicoes);
  const apagar = useCaso(TOKENS_DA_MASSA.ApagarDistribuicao);
  const consulta = useConsulta(CHAVES.distribuicoes, () => listar.executar());
  const [edicao, setEdicao] = useState<{ readonly versao: number; readonly inicial: DistribuicaoNomeada }>({
    versao: 0,
    inicial: NOVA,
  });
  const [aviso, setAviso] = useState<string | null>(null);
  const abrir = (inicial: DistribuicaoNomeada) => {
    setAviso(null);
    setEdicao((atual) => ({ versao: atual.versao + 1, inicial }));
  };
  const acaoDeApagar = useAcao((nome: string) => apagar.executar(nome), {
    invalidar: [CHAVES.distribuicoes],
    aoTerminar: () => abrir(NOVA),
  });

  return (
    <>
      <Titulo ajuda="distribuicoes">{t('massa.distribuicoes.titulo')}</Titulo>
      <Secao titulo={t('massa.distribuicoes.lista')}>
        <ListaDeErros falha={acaoDeApagar.falha} />
        <Resultado consulta={consulta}>
          {(salvas) =>
            salvas.length === 0 ? (
              <p>{t('massa.distribuicoes.vazio')}</p>
            ) : (
              <Tabela rotulo={t('massa.distribuicoes.lista')}>
                <thead>
                  <tr>
                    <th>{t('massa.distribuicoes.colunas.nome')}</th>
                    <th>{t('massa.distribuicoes.colunas.bancos')}</th>
                    <th>{t('massa.distribuicoes.colunas.perfis')}</th>
                    <th>{t('massa.distribuicoes.colunas.pjPct')}</th>
                    <th>{t('massa.distribuicoes.colunas.acoes')}</th>
                  </tr>
                </thead>
                <tbody>
                  {salvas.map((salva) => (
                    <tr key={salva.nome}>
                      <td className="font-mono text-xs">{salva.nome}</td>
                      <td>{resumir(salva.distribuicao.bancos)}</td>
                      <td>{resumir(salva.distribuicao.perfis)}</td>
                      <td>{salva.distribuicao.pjPct}</td>
                      <td>
                        <div className="flex gap-2">
                          <Botao variante="secundario" onClick={() => abrir(salva)}>
                            {t('massa.distribuicoes.editar', { nome: salva.nome })}
                          </Botao>
                          <Confirmacao
                            gatilho={t('massa.distribuicoes.apagar', { nome: salva.nome })}
                            titulo={t('massa.distribuicoes.apagarTitulo', { nome: salva.nome })}
                            aoConfirmar={() => acaoDeApagar.executar(salva.nome)}
                            desabilitado={acaoDeApagar.ocupado}
                          >
                            {t('massa.distribuicoes.apagarTexto')}
                          </Confirmacao>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Tabela>
            )
          }
        </Resultado>
        <Botao className="mt-3" variante="secundario" onClick={() => abrir(NOVA)}>
          {t('massa.distribuicoes.nova')}
        </Botao>
      </Secao>
      <Secao titulo={edicao.inicial.nome === '' ? t('massa.distribuicoes.nova') : t('massa.distribuicoes.edicao')}>
        {aviso !== null && (
          <p role="status" className="mb-2 text-sm text-teal-800">
            {aviso}
          </p>
        )}
        <EditorDeDistribuicao
          key={edicao.versao}
          inicial={edicao.inicial}
          aoSalvar={() => setAviso(t('massa.distribuicoes.salva'))}
        />
      </Secao>
    </>
  );
};
