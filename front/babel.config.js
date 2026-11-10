// Babel transpila; a checagem de tipos fica com o fork-ts-checker (guia de front-end).
module.exports = {
  presets: [
    ['@babel/preset-env', { targets: 'defaults' }],
    ['@babel/preset-react', { runtime: 'automatic' }],
    '@babel/preset-typescript',
  ],
  plugins: [['@babel/plugin-proposal-decorators', { legacy: true }]],
  env: {
    development: { plugins: ['react-refresh/babel'] },
  },
};
