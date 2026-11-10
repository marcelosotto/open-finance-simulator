import type { Config } from 'jest';

// Fuso fixo e diferente de UTC: as conversões de hora local para UTC aparecem nos testes (revisão da 010).
process.env.TZ = 'America/Sao_Paulo';

// 100% no core e 80% no total, travado aqui (guia de front-end, regra 8).
const config: Config = {
  testEnvironment: '<rootDir>/tests/ambiente-jsdom.js',
  testEnvironmentOptions: { customExportConditions: [''] },
  roots: ['<rootDir>/tests'],
  testMatch: ['**/*.test.ts', '**/*.test.tsx'],
  setupFiles: ['<rootDir>/tests/antes.ts'],
  setupFilesAfterEnv: ['<rootDir>/tests/setup.ts'],
  transform: {
    '^.+\\.tsx?$': ['ts-jest', { tsconfig: '<rootDir>/tsconfig.json' }],
    // Dependências do msw publicadas só em ESM.
    '^.+\\.m?js$': 'babel-jest',
  },
  // Transforma os .mjs e os pacotes ESM do msw; o resto de node_modules fica como está.
  transformIgnorePatterns: ['/node_modules/(?!(until-async)/).+(?<!\\.mjs)$'],
  moduleNameMapper: { '^@/(.*)$': '<rootDir>/src/$1', '\\.(s?css)$': '<rootDir>/tests/estilo.ts' },
  collectCoverageFrom: ['src/**/*.{ts,tsx}', '!src/main/index.tsx', '!src/**/*.d.ts'],
  coverageReporters: ['text-summary', 'lcov', 'cobertura'],
  coverageThreshold: {
    global: { statements: 80, branches: 80, functions: 80, lines: 80 },
    './src/core/': { statements: 100, branches: 100, functions: 100, lines: 100 },
  },
  reporters: [
    'default',
    ['jest-html-reporter', { outputPath: 'relatorios/testes.html', pageTitle: 'Testes do back-office' }],
  ],
  testResultsProcessor: 'jest-sonar-reporter',
  watchPlugins: ['jest-watch-typeahead/filename', 'jest-watch-typeahead/testname'],
};

export default config;
