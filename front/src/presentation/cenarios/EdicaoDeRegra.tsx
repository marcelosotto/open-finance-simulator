import { useId, type ReactNode } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TOKENS_DOS_CENARIOS } from '@/application/cenarios/casos';
import { TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import type { Falha } from '@/core/comum/falha';
import { formularioDaRegra, formularioVazio, type FormularioDeRegra } from '@/core/cenarios/formulario';
import {
  ESTADOS_DE_RECURSO,
  FALHAS_DE_TRANSPORTE,
  RESULTADOS_DE_INDISPONIBILIDADE,
  TIPOS_DE_EFEITO,
  aceitaCampo,
  aceitaQuantidade,
  type ItemDoCatalogo,
  type PacoteDeCenario,
} from '@/core/cenarios/regra';
import type { ResumoDeInstituicao } from '@/core/instituicoes/instituicao';
import { useCaso } from '../comum/casos';
import { Resultado, useAcao, useConsulta } from '../comum/consultas';
import { Campo, Selecao } from '../comum/campo';
import { SeletorDeCliente } from '../massa/SeletorDeCliente';
import { Botao } from '../comum/botao';
import { ListaDeErros } from '../comum/estado';
import { Secao, Titulo } from '../comum/titulo';
import { CHAVES } from './chaves';
import { useDescricaoDaRegra } from './descricao';
import { useSugestoesDeEndpoint } from './endpoints';

interface Opcoes {
  readonly bancos: readonly ResumoDeInstituicao[];
  readonly pacotes: readonly PacoteDeCenario[];
  readonly defeitos: readonly ItemDoCatalogo[];
}

/** Bancos, pacotes e catálogo de defeitos, que o formulário precisa antes de abrir. */
const ComOpcoes = ({ children }: { readonly children: (opcoes: Opcoes) => ReactNode }) => {
  const listarBancos = useCaso(TOKENS_DAS_INSTITUICOES.Listar);
  const listarPacotes = useCaso(TOKENS_DOS_CENARIOS.ListarPacotes);
  const catalogo = useCaso(TOKENS_DOS_CENARIOS.Defeitos);
  const bancos = useConsulta(CHAVES.instituicoes, () => listarBancos.executar());
  const pacotes = useConsulta(CHAVES.pacotes, () => listarPacotes.executar());
  const defeitos = useConsulta(CHAVES.defeitos, () => catalogo.executar());
  return (
    <Resultado consulta={bancos}>
      {(listaDeBancos) => (
        <Resultado consulta={pacotes}>
          {(listaDePacotes) => (
            <Resultado consulta={defeitos}>
              {(listaDeDefeitos) =>
                children({ bancos: listaDeBancos, pacotes: listaDePacotes, defeitos: listaDeDefeitos })
              }
            </Resultado>
          )}
        </Resultado>
      )}
    </Resultado>
  );
};

/** Os valores da lista, com o atual no fim se ele não estiver nela (banco ou pacote apagado). */
const comAtual = (valores: readonly string[], atual: string): readonly string[] =>
  atual === '' || valores.includes(atual) ? valores : [...valores, atual];

interface PropsDoFormulario {
  readonly inicial: FormularioDeRegra;
  readonly opcoes: Opcoes;
  readonly ocupado: boolean;
  readonly falha: Falha | null;
  readonly aoSalvar: (formulario: FormularioDeRegra) => void;
}

/** Alcance, efeito com só os campos do tipo, e quando vale (UC21, RN111). */
const FormularioDaRegra = ({ inicial, opcoes, ocupado, falha, aoSalvar }: PropsDoFormulario) => {
  const { t } = useTranslation();
  const idDaLigada = useId();
  const endpoints = useSugestoesDeEndpoint();
  const { register, handleSubmit, watch, setValue } = useForm<FormularioDeRegra>({ defaultValues: inicial });
  const tipo = watch('tipo');
  const defeito = watch('defeito');
  const nomesDosBancos = new Map(opcoes.bancos.map((banco) => [banco.endereco, banco.nome]));
  const bancos = comAtual([...nomesDosBancos.keys()], inicial.instituicao);
  const pacotes = comAtual(
    opcoes.pacotes.map((p) => p.nome),
    inicial.pacote,
  );
  const descricaoDoDefeito = opcoes.defeitos.find((item) => item.nome === defeito)?.descricao;
  const f = (campo: string) => t(`cenarios.formulario.${campo}`);
  const espera = <Campo rotulo={f('esperaMs')} inputMode="numeric" {...register('esperaMs')} />;

  return (
    <form onSubmit={handleSubmit(aoSalvar)} className="max-w-3xl">
      <Secao titulo={f('alcance')}>
        <p className="mb-3 text-sm text-slate-600">{f('alcanceAjuda')}</p>
        <div className="grid gap-3 md:grid-cols-2">
          <Selecao rotulo={f('instituicao')} {...register('instituicao')}>
            <option value="">{t('cenarios.qualquer')}</option>
            {bancos.map((endereco) => (
              <option key={endereco} value={endereco}>
                {nomesDosBancos.get(endereco) ?? endereco}
              </option>
            ))}
          </Selecao>
          <Campo rotulo={f('endpoint')} list={endpoints.lista} {...register('endpoint')} />
          <SeletorDeCliente
            opcional
            banco={watch('instituicao')}
            nomeDoBanco={nomesDosBancos.get(watch('instituicao'))}
            rotulo={f('cliente')}
            valor={watch('cliente')}
            aoMudar={(documento) => setValue('cliente', documento)}
          />
          <Campo rotulo={f('recurso')} {...register('recurso')} />
          {endpoints.sugestoes}
        </div>
      </Secao>

      <Secao titulo={f('efeito')}>
        <div className="grid gap-3 md:grid-cols-2">
          <Selecao rotulo={f('tipo')} {...register('tipo')}>
            {TIPOS_DE_EFEITO.map((valor) => (
              <option key={valor} value={valor}>
                {t(`cenarios.efeitos.${valor}`)}
              </option>
            ))}
          </Selecao>
          {tipo === 'latencia' && <Campo rotulo={f('latenciaMs')} inputMode="numeric" {...register('latenciaMs')} />}
          {tipo === 'status' && (
            <>
              <Campo rotulo={f('status')} inputMode="numeric" {...register('status')} />
              {espera}
            </>
          )}
          {tipo === 'timeout' && espera}
          {tipo === 'indisponibilidade' && (
            <>
              <Selecao rotulo={f('resultado')} {...register('resultado')}>
                <option value="">{t('cenarios.escolha')}</option>
                {RESULTADOS_DE_INDISPONIBILIDADE.map((valor) => (
                  <option key={valor} value={valor}>
                    {t(`cenarios.resultados.${valor}`)}
                  </option>
                ))}
              </Selecao>
              {espera}
            </>
          )}
          {tipo === 'transporte' && (
            <Selecao rotulo={f('falha')} {...register('falha')}>
              <option value="">{t('cenarios.escolha')}</option>
              {FALHAS_DE_TRANSPORTE.map((valor) => (
                <option key={valor} value={valor}>
                  {t(`cenarios.falhasDeTransporte.${valor}`)}
                </option>
              ))}
            </Selecao>
          )}
          {tipo === 'estado' && (
            <Selecao rotulo={f('estado')} {...register('estado')}>
              <option value="">{t('cenarios.escolha')}</option>
              {ESTADOS_DE_RECURSO.map((valor) => (
                <option key={valor} value={valor}>
                  {valor}
                </option>
              ))}
            </Selecao>
          )}
          {tipo === 'defeito' && (
            <>
              <Selecao rotulo={f('defeito')} {...register('defeito')}>
                <option value="">{t('cenarios.escolha')}</option>
                {opcoes.defeitos.map((item) => (
                  <option key={item.nome} value={item.nome}>
                    {item.nome}
                  </option>
                ))}
              </Selecao>
              {aceitaCampo(defeito) && <Campo rotulo={f('campo')} {...register('campo')} />}
              {aceitaQuantidade(defeito) && (
                <Campo rotulo={f('quantidade')} inputMode="numeric" {...register('quantidade')} />
              )}
              {descricaoDoDefeito !== undefined && (
                <p className="text-sm text-slate-600 md:col-span-2">{descricaoDoDefeito}</p>
              )}
            </>
          )}
        </div>
      </Secao>

      <Secao titulo={f('quando')}>
        <div className="grid gap-3 md:grid-cols-2">
          <Campo
            rotulo={f('frequenciaPct')}
            inputMode="decimal"
            ajuda={tipo === 'estado' ? f('frequenciaDoEstado') : undefined}
            {...register('frequenciaPct')}
          />
          <Campo rotulo={f('semente')} inputMode="numeric" {...register('semente')} />
          <Campo rotulo={f('inicio')} placeholder="2026-12-01T00:00:00Z" {...register('inicio')} />
          <Campo rotulo={f('fim')} placeholder="2026-12-31T23:59:59Z" {...register('fim')} />
          <Selecao rotulo={f('pacote')} {...register('pacote')}>
            <option value="">{f('semPacote')}</option>
            {pacotes.map((nome) => (
              <option key={nome} value={nome}>
                {nome}
              </option>
            ))}
          </Selecao>
          <Campo rotulo={f('nome')} {...register('nome')} />
          <div className="flex items-center gap-2">
            <input id={idDaLigada} type="checkbox" {...register('ligada')} />
            <label htmlFor={idDaLigada} className="text-sm">
              {f('ligada')}
            </label>
          </div>
        </div>
      </Secao>

      <div className="space-y-3">
        <ListaDeErros falha={falha} />
        <Botao type="submit" disabled={ocupado}>
          {f('salvar')}
        </Botao>
      </div>
    </form>
  );
};

export const NovaRegra = () => {
  const { t } = useTranslation();
  const navegar = useNavigate();
  const criar = useCaso(TOKENS_DOS_CENARIOS.CriarRegra);
  const acao = useAcao((formulario: FormularioDeRegra) => criar.executar(formulario), {
    invalidar: [CHAVES.regras],
    aoTerminar: () => navegar('/cenarios'),
  });
  return (
    <>
      <Titulo ajuda="regra">{t('cenarios.formulario.tituloNova')}</Titulo>
      <ComOpcoes>
        {(opcoes) => (
          <FormularioDaRegra
            inicial={formularioVazio()}
            opcoes={opcoes}
            ocupado={acao.ocupado}
            falha={acao.falha}
            aoSalvar={(formulario) => acao.executar(formulario)}
          />
        )}
      </ComOpcoes>
    </>
  );
};

/** O PUT troca a regra inteira, então o formulário leva também se ela está ligada. */
export const EdicaoDeRegra = () => {
  const { t } = useTranslation();
  const navegar = useNavigate();
  const { id = '' } = useParams();
  const descricao = useDescricaoDaRegra();
  const obter = useCaso(TOKENS_DOS_CENARIOS.ObterRegra);
  const alterar = useCaso(TOKENS_DOS_CENARIOS.AlterarRegra);
  const consulta = useConsulta(CHAVES.regra(id), () => obter.executar(id));
  const acao = useAcao((formulario: FormularioDeRegra) => alterar.executar({ id, formulario }), {
    invalidar: [CHAVES.regras],
    aoTerminar: () => navegar('/cenarios'),
  });
  return (
    <Resultado consulta={consulta}>
      {(regra) => (
        <>
          <Titulo ajuda="regra">{t('cenarios.formulario.tituloEdicao', { regra: descricao.rotulo(regra) })}</Titulo>
          <ComOpcoes>
            {(opcoes) => (
              <FormularioDaRegra
                inicial={formularioDaRegra(regra)}
                opcoes={opcoes}
                ocupado={acao.ocupado}
                falha={acao.falha}
                aoSalvar={(formulario) => acao.executar(formulario)}
              />
            )}
          </ComOpcoes>
        </>
      )}
    </Resultado>
  );
};
