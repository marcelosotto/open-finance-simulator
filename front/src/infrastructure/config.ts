import { z } from 'zod';

// Lida pelo dotenv-webpack e validada na subida. Vazio: mesma origem (o simulador serve o front na 8081).
const esquema = z.object({ SIM_ADMIN_API_URL: z.string().url().or(z.literal('')).default('') });

export interface Configuracao {
  readonly urlDaAdministracao: string;
}

export const lerConfiguracao = (variaveis: Record<string, string | undefined>): Configuracao => {
  const lido = esquema.parse({ SIM_ADMIN_API_URL: variaveis.SIM_ADMIN_API_URL ?? '' });
  return { urlDaAdministracao: lido.SIM_ADMIN_API_URL.replace(/\/$/, '') };
};
