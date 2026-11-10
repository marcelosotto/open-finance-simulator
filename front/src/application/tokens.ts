// Token tipado para o container, sem amarrar a application ao tsyringe.
export type Token<T> = symbol & { readonly __tipo?: T };

export const token = <T>(nome: string): Token<T> => Symbol(nome) as Token<T>;
