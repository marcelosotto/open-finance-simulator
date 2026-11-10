// dotenv-webpack não traz tipos; só o construtor é usado.
declare module 'dotenv-webpack' {
  import type { WebpackPluginInstance } from 'webpack';

  interface OpcoesDoDotenv {
    systemvars?: boolean;
    safe?: boolean;
    defaults?: boolean;
    allowEmptyValues?: boolean;
  }

  export default class Dotenv implements WebpackPluginInstance {
    constructor(opcoes?: OpcoesDoDotenv);
    apply: WebpackPluginInstance['apply'];
  }
}
