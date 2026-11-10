import { merge } from 'webpack-merge';
import type { Configuration } from 'webpack';
import 'webpack-dev-server';
import ReactRefreshWebpackPlugin from '@pmmmwh/react-refresh-webpack-plugin';
import { comum } from './webpack.common';

// Desenvolvimento: porta 3000, com /admin repassado ao simulador na 8081 (mesma origem, sem CORS).
const configuracao: Configuration = merge(comum, {
  mode: 'development',
  devtool: 'eval-source-map',
  module: {
    rules: [{ test: /\.(s?css)$/, use: ['style-loader', 'css-loader', 'postcss-loader', 'sass-loader'] }],
  },
  plugins: [new ReactRefreshWebpackPlugin()],
  devServer: {
    port: 3000,
    historyApiFallback: true,
    hot: true,
    proxy: [{ context: ['/admin'], target: process.env.SIM_ADMIN_PROXY ?? 'http://localhost:8081' }],
  },
});

export default configuracao;
