import { useMutation, useQuery, useQueryClient, type QueryKey } from '@tanstack/react-query';
import { useState, type ReactNode } from 'react';
import type { Either } from '@/core/comum/either';
import type { Falha } from '@/core/comum/falha';
import { Carregando, ErroComNovaTentativa } from './estado';

/** Leitura pela API: o dado é o próprio Either, tratado na tela sem try/catch. */
export const useConsulta = <T,>(
  chave: QueryKey,
  executar: () => Promise<Either<Falha, T>>,
  opcoes: { readonly intervaloMs?: number | false; readonly ativa?: boolean } = {},
) =>
  useQuery({
    queryKey: chave,
    queryFn: executar,
    refetchInterval: opcoes.intervaloMs ?? false,
    enabled: opcoes.ativa ?? true,
  });

interface PropsDoResultado<T> {
  readonly consulta: { readonly data?: Either<Falha, T> | undefined; readonly refetch: () => unknown };
  readonly children: (valor: T) => ReactNode;
  /** O que faltou e como resolver quando a administração responde 404 (UI19): nunca só "não encontrado". */
  readonly seInexistente?: ReactNode;
}

export const Resultado = <T,>({ consulta, children, seInexistente }: PropsDoResultado<T>) => {
  const dado = consulta.data;
  if (dado === undefined) return <Carregando />;
  if (dado.isLeft()) {
    return (
      <ErroComNovaTentativa
        falha={dado.value}
        explicacao={dado.value.tipo === 'inexistente' ? seInexistente : undefined}
        aoTentar={() => consulta.refetch()}
      />
    );
  }
  return <>{children(dado.value)}</>;
};

interface OpcoesDaAcao<T> {
  readonly invalidar?: readonly QueryKey[];
  readonly aoTerminar?: (valor: T) => void;
}

/** Escrita: guarda a última falha para a tela mostrar sem perder o que foi digitado (FR-006). */
export const useAcao = <A, T>(executar: (argumento: A) => Promise<Either<Falha, T>>, opcoes: OpcoesDaAcao<T> = {}) => {
  const cliente = useQueryClient();
  const [falha, setFalha] = useState<Falha | null>(null);
  const mutacao = useMutation({
    mutationFn: executar,
    onSuccess: async (resultado) => {
      if (resultado.isLeft()) {
        setFalha(resultado.value);
        return;
      }
      setFalha(null);
      await Promise.all((opcoes.invalidar ?? []).map((queryKey) => cliente.invalidateQueries({ queryKey })));
      opcoes.aoTerminar?.(resultado.value);
    },
  });
  return { executar: mutacao.mutate, ocupado: mutacao.isPending, falha, limpar: () => setFalha(null) };
};
