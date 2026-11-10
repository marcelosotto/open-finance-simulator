import { container } from 'tsyringe';
import type { Token } from '@/application/tokens';

/** O caso de uso vem do container; a presentation nunca cria repositório (guia, camadas). */
export const useCaso = <T>(token: Token<T>): T => container.resolve<T>(token);
