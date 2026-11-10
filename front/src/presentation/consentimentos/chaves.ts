export const CHAVES = {
  opcoes: (cliente: string, banco: string) => ['consentimentos', 'opcoes', cliente, banco] as const,
  visao: (consentId: string) => ['consentimentos', consentId] as const,
  receptoras: ['receptoras'] as const,
};
