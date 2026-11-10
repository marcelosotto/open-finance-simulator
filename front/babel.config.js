// Babel transpila; a checagem de tipos fica com o fork-ts-checker (guia de front-end).
module.exports = {
  presets: [
    ['@babel/preset-env', { targets: 'defaults' }],
    ['@babel/preset-react', { runtime: 'automatic' }],
    '@babel/preset-typescript',
  ],
  plugins: [
    ['@babel/plugin-proposal-decorators', { legacy: true }],
    // Só no webpack-dev-server (webpack.dev.ts liga SIM_REFRESH); nunca no build de produção nem no Jest.
    ...(process.env.SIM_REFRESH === '1' ? ['react-refresh/babel'] : []),
  ],
};
