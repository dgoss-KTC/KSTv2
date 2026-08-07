/// <reference types="vitest/globals" />
import '@testing-library/jest-dom';

// Provide a minimal window.__KST_BACKEND_URL__ for tests
Object.defineProperty(window, '__KST_BACKEND_URL__', {
  value: 'http://127.0.0.1:19999',
  writable: true,
});

// jsdom (v26) defers to Node's built-in, opt-in-only localStorage implementation, which is
// unusable in tests without a `--localstorage-file` flag. Polyfill a simple in-memory Storage
// so app code that reads/writes window.localStorage works the same as it does in a real browser.
class InMemoryStorage implements Storage {
  private store = new Map<string, string>();

  get length(): number {
    return this.store.size;
  }

  clear(): void {
    this.store.clear();
  }

  getItem(key: string): string | null {
    return this.store.has(key) ? this.store.get(key)! : null;
  }

  key(index: number): string | null {
    return Array.from(this.store.keys())[index] ?? null;
  }

  removeItem(key: string): void {
    this.store.delete(key);
  }

  setItem(key: string, value: string): void {
    this.store.set(key, String(value));
  }
}

Object.defineProperty(window, 'localStorage', {
  value: new InMemoryStorage(),
  writable: true,
});
