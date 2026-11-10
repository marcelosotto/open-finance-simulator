// Lint do front. As regras de camada seguem a tabela do guia de front-end (core → application → infrastructure → presentation).
import js from '@eslint/js';
import tseslint from 'typescript-eslint';
import react from 'eslint-plugin-react';
import reactHooks from 'eslint-plugin-react-hooks';
import jest from 'eslint-plugin-jest';
import testingLibrary from 'eslint-plugin-testing-library';
import globals from 'globals';

const camadas = {
  application: { group: ['**/application/**'], message: 'Camada de fora: use só core aqui.' },
  infrastructure: {
    group: ['**/infrastructure/**'],
    message: 'A infrastructure fica fora desta camada; receba pelo container.',
  },
  presentation: { group: ['**/presentation/**'], message: 'A presentation é a camada mais externa.' },
  main: { group: ['**/main/**'], message: 'O main só monta o container e as rotas.' },
};
const bibliotecas = (nomes, motivo) => nomes.map((name) => ({ name, message: motivo }));
const restringir = (paths, patterns) => ({ 'no-restricted-imports': ['error', { paths, patterns }] });
const semFramework = bibliotecas(
  ['react', 'react-dom', 'axios', 'zod', 'tsyringe', 'zustand', '@tanstack/react-query'],
  'Fora desta camada (guia de front-end).',
);

export default tseslint.config(
  {
    ignores: ['dist/**', 'coverage/**', 'relatorios/**', 'node_modules/**', 'test-results/**', 'playwright-report/**'],
  },
  js.configs.recommended,
  ...tseslint.configs.strict,
  {
    files: ['**/*.{ts,tsx}'],
    languageOptions: { globals: { ...globals.browser, ...globals.node } },
    plugins: { react, 'react-hooks': reactHooks },
    settings: { react: { version: 'detect' } },
    rules: {
      ...react.configs.recommended.rules,
      ...react.configs['jsx-runtime'].rules,
      ...reactHooks.configs.recommended.rules,
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/consistent-type-imports': 'error',
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_', varsIgnorePattern: '^_' }],
    },
  },
  {
    files: ['**/*.js', '**/*.cjs'],
    languageOptions: { globals: globals.node, sourceType: 'commonjs' },
    rules: { '@typescript-eslint/no-require-imports': 'off' },
  },
  {
    files: ['src/core/**'],
    rules: restringir(semFramework, [camadas.application, camadas.infrastructure, camadas.presentation, camadas.main]),
  },
  {
    files: ['src/application/**'],
    rules: restringir(semFramework, [camadas.infrastructure, camadas.presentation, camadas.main]),
  },
  {
    files: ['src/infrastructure/**'],
    rules: restringir(
      bibliotecas(['react', 'react-dom', 'zustand', '@tanstack/react-query'], 'Fora da infrastructure.'),
      [camadas.presentation, camadas.main],
    ),
  },
  {
    files: ['src/presentation/**'],
    rules: restringir(bibliotecas(['axios', 'zod'], 'A presentation recebe dados pelos casos de uso.'), [
      camadas.infrastructure,
      camadas.main,
    ]),
  },
  {
    files: ['tests/**'],
    ...jest.configs['flat/recommended'],
    rules: { ...jest.configs['flat/recommended'].rules, '@typescript-eslint/no-non-null-assertion': 'off' },
  },
  { files: ['tests/presentation/**'], ...testingLibrary.configs['flat/react'] },
);
