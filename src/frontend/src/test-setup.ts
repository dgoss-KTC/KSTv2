/// <reference types="vitest/globals" />
import '@testing-library/jest-dom';

// Provide a minimal window.__KST_BACKEND_URL__ for tests
Object.defineProperty(window, '__KST_BACKEND_URL__', {
  value: 'http://127.0.0.1:19999',
  writable: true,
});
