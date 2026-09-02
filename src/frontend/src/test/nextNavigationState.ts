import { vi } from 'vitest';

export interface MockRouter {
  push: ReturnType<typeof vi.fn>;
  replace: ReturnType<typeof vi.fn>;
  refresh: ReturnType<typeof vi.fn>;
  back: ReturnType<typeof vi.fn>;
  forward: ReturnType<typeof vi.fn>;
  prefetch: ReturnType<typeof vi.fn>;
}

export interface MockNavigationState {
  router: MockRouter;
  pathname: string;
  searchParams: URLSearchParams;
}

const STATE_KEY = '__sbNextNavigationMockState__';

function createState(): MockNavigationState {
  return {
    router: {
      push: vi.fn(),
      replace: vi.fn(),
      refresh: vi.fn(),
      back: vi.fn(),
      forward: vi.fn(),
      prefetch: vi.fn(),
    },
    pathname: '/',
    searchParams: new URLSearchParams(),
  };
}

export function getNavigationState(): MockNavigationState {
  const globalRef = globalThis as unknown as Record<string, MockNavigationState>;
  if (!globalRef[STATE_KEY]) {
    globalRef[STATE_KEY] = createState();
  }
  return globalRef[STATE_KEY];
}

export function getMockRouter(): MockRouter {
  return getNavigationState().router;
}

export function setMockPathname(path: string): void {
  getNavigationState().pathname = path;
}

export function setMockSearchParams(
  params: URLSearchParams | string | Record<string, string>,
): void {
  const state = getNavigationState();
  if (params instanceof URLSearchParams) {
    state.searchParams = params;
    return;
  }
  if (typeof params === 'string') {
    state.searchParams = new URLSearchParams(params);
    return;
  }
  const next = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    next.append(key, value);
  }
  state.searchParams = next;
}

export function resetNavigationMocks(): void {
  const state = getNavigationState();
  state.router.push.mockReset();
  state.router.replace.mockReset();
  state.router.refresh.mockReset();
  state.router.back.mockReset();
  state.router.forward.mockReset();
  state.router.prefetch.mockReset();
  state.pathname = '/';
  state.searchParams = new URLSearchParams();
}
