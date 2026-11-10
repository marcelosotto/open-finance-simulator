import '@testing-library/jest-dom';
import { servidor } from './apoio/servidor';

beforeAll(() => servidor.listen({ onUnhandledRequest: 'error' }));
afterEach(() => servidor.resetHandlers());
afterAll(() => servidor.close());
