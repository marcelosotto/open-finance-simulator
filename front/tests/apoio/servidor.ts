import { setupServer } from 'msw/node';

// Servidor msw único; cada teste acrescenta os seus handlers com servidor.use(...).
export const servidor = setupServer();
