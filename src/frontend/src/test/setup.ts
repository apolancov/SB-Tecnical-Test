import '@testing-library/jest-dom/vitest';
import { afterEach, beforeEach, vi } from 'vitest';
import { cleanup } from '@testing-library/react';
import {
  getNavigationState,
  resetNavigationMocks,
} from './nextNavigationState';

beforeEach(() => {
  sessionStorage.clear();
  localStorage.clear();
  resetNavigationMocks();
});

afterEach(() => {
  cleanup();
  sessionStorage.clear();
  localStorage.clear();
});

vi.mock('next/navigation', () => {
  const state = getNavigationState();
  return {
    useRouter: () => state.router,
    usePathname: () => state.pathname,
    useSearchParams: () => state.searchParams,
  };
});
