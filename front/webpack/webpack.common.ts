import path from 'node:path';
import type { Configuration } from 'webpack';
import HtmlWebpackPlugin from 'html-webpack-plugin';
import CopyWebpackPlugin from 'copy-webpack-plugin';
import Dotenv from 'dotenv-webpack';
import ForkTsCheckerWebpackPlugin from 'fork-ts-checker-webpack-plugin';

const raiz = path.resolve(__dirname, '..');

export const comum: Configuration = {
  entry: path.join(raiz, 'src/main/index.tsx'),
  output: {
    path: path.join(raiz, 'dist'),
    publicPath: '/',
    clean: true,
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js'],
    alias: { '@': path.join(raiz, 'src') },
  },
  module: {
    rules: [
      { test: /\.[jt]sx?$/, exclude: /node_modules/, use: 'babel-loader' },
      { test: /\.(png|svg|jpe?g|gif|woff2?)$/i, type: 'asset' },
    ],
  },
  plugins: [
    new HtmlWebpackPlugin({
      template: path.join(raiz, 'public/index.html'),
      favicon: path.join(raiz, 'public/favicon.svg'),
    }),
    new CopyWebpackPlugin({ patterns: [{ from: path.join(raiz, 'public/robots.txt') }] }),
    new Dotenv({ systemvars: true, safe: false, defaults: false, allowEmptyValues: true }),
    new ForkTsCheckerWebpackPlugin({ typescript: { configFile: path.join(raiz, 'tsconfig.json') } }),
  ],
};
