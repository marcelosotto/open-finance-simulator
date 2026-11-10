import { container } from 'tsyringe';
import type { Token } from '@/application/tokens';
import { ObterAmbiente, TOKENS_DO_AMBIENTE } from '@/application/ambiente/obter-ambiente';
import { casosDasInstituicoes } from '@/application/instituicoes/casos';
import { casosDosConsentimentos } from '@/application/consentimentos/casos';
import { casosDaMassa } from '@/application/massa/casos';
import { casosDosCenarios } from '@/application/cenarios/casos';
import type { ClienteAdmin } from '@/infrastructure/http/cliente-admin';
import { RepositorioDoAmbienteHttp } from '@/infrastructure/ambiente/repositorio-http';
import { RepositorioDeInstituicoesHttp } from '@/infrastructure/instituicoes/repositorio-http';
import { RepositorioDeConsentimentosHttp } from '@/infrastructure/consentimentos/repositorio-http';
import { RepositorioDaMassaHttp } from '@/infrastructure/massa/repositorio-http';
import { RepositorioDeCenariosHttp } from '@/infrastructure/cenarios/repositorio-http';

const registrarTodos = (pares: readonly (readonly [Token<unknown>, unknown])[]) => {
  for (const [token, caso] of pares) container.registerInstance(token, caso);
};

/** Monta o container: cada caso de uso recebe o repositório HTTP da sua área (guia, injeção de dependência). */
export const registrar = (cliente: ClienteAdmin): void => {
  container.registerInstance(
    TOKENS_DO_AMBIENTE.ObterAmbiente,
    new ObterAmbiente(new RepositorioDoAmbienteHttp(cliente)),
  );
  registrarTodos(casosDasInstituicoes(new RepositorioDeInstituicoesHttp(cliente)));
  registrarTodos(casosDosConsentimentos(new RepositorioDeConsentimentosHttp(cliente)));
  registrarTodos(casosDaMassa(new RepositorioDaMassaHttp(cliente)));
  registrarTodos(casosDosCenarios(new RepositorioDeCenariosHttp(cliente)));
};
