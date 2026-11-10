import { merge } from 'webpack-merge';
import MiniCssExtractPlugin from 'mini-css-extract-plugin';
import { comum } from './webpack.common';

export default merge(comum, {
  mode: 'production',
  devtool: 'source-map',
  output: { filename: 'js/[name].[contenthash].js', chunkFilename: 'js/[name].[contenthash].js' },
  module: {
    rules: [{ test: /\.(s?css)$/, use: [MiniCssExtractPlugin.loader, 'css-loader', 'postcss-loader', 'sass-loader'] }],
  },
  plugins: [new MiniCssExtractPlugin({ filename: 'css/[name].[contenthash].css' })],
  performance: { maxAssetSize: 1_000_000, maxEntrypointSize: 1_000_000 },
});
