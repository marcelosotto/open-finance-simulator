import { useTranslation } from 'react-i18next';

type Grupo = 'estados' | 'alcadas' | 'decisoes' | 'rejeitadoPor' | 'grupos';

/** Nome em português de um código da API; o código desconhecido aparece como veio. */
export const useRotulos = () => {
  const { t } = useTranslation();
  const rotulo = (grupo: Grupo, codigo: string) => t(`rotulos.${grupo}.${codigo}`, { defaultValue: codigo });
  return {
    estado: (codigo: string) => rotulo('estados', codigo),
    alcada: (codigo: string) => rotulo('alcadas', codigo),
    decisao: (codigo: string) => rotulo('decisoes', codigo),
    rejeitadoPor: (codigo: string) => rotulo('rejeitadoPor', codigo),
    grupo: (codigo: string) => rotulo('grupos', codigo),
  };
};

/** Estado em português, com o código da API no title para quem precisa dele. */
export const Estado = ({ codigo }: { readonly codigo: string }) => {
  const rotulos = useRotulos();
  return <span title={codigo}>{rotulos.estado(codigo)}</span>;
};
