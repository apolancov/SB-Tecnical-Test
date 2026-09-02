import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { useInstitutionMutation } from './useInstitutionMutation';
import type { Institution, InstitutionInput } from '../types/institution';

function buildService(overrides: Partial<{
  create: (input: InstitutionInput) => Promise<Institution>;
  update: (id: string, input: InstitutionInput) => Promise<Institution>;
  remove: (id: string) => Promise<void>;
}> = {}) {
  return {
    search: vi.fn(),
    getFilterOptions: vi.fn(),
    getById: vi.fn(),
    create: overrides.create ?? vi.fn(async () => sample),
    update: overrides.update ?? vi.fn(async () => sample),
    remove: overrides.remove ?? vi.fn(async () => undefined),
  };
}

const sample: Institution = {
  id: 'abc-123',
  name: 'Sample',
  category: 'Cat',
  statePower: 'Poder Ejecutivo',
  sector: 'Sector',
};

const sampleInput: InstitutionInput = {
  name: sample.name,
  category: sample.category,
  statePower: sample.statePower,
  sector: sample.sector,
};

function describeApiError(kind: string, message: string) {
  return { kind, status: 400, message, code: 'test.code' };
}

describe('useInstitutionMutation', () => {
  beforeEach(() => {
    vi.useRealTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('create returns the institution on success and notifies the callback', async () => {
    const service = buildService();
    const onCreated = vi.fn();

    const { result } = renderHook(() =>
      useInstitutionMutation({ service, callbacks: { onCreated } }),
    );

    let created: Institution | null = null;
    await act(async () => {
      created = await result.current.create(sampleInput);
    });

    expect(created).toEqual(sample);
    expect(onCreated).toHaveBeenCalledWith(sample);
    expect(result.current.lastCreated).toEqual(sample);
    expect(result.current.error).toBeNull();
    expect(result.current.submitting).toBe(false);
  });

  it('create surfaces a friendly message when the service throws Validation', async () => {
    const service = buildService({
      create: vi.fn(async () => {
        throw describeApiError('Validation', 'El nombre es obligatorio.');
      }),
    });

    const { result } = renderHook(() =>
      useInstitutionMutation({ service }),
    );

    let created: Institution | null = null;
    await act(async () => {
      created = await result.current.create(sampleInput);
    });

    expect(created).toBeNull();
    expect(result.current.error).toBe('El nombre es obligatorio.');
    expect(result.current.submitting).toBe(false);
  });

  it('create surfaces a Conflict error from the API', async () => {
    const service = buildService({
      create: vi.fn(async () => {
        throw describeApiError(
          'Conflict',
          'Ya existe una institución con el mismo nombre.',
        );
      }),
    });

    const { result } = renderHook(() =>
      useInstitutionMutation({ service }),
    );

    await act(async () => {
      await result.current.create(sampleInput);
    });

    expect(result.current.error).toBe('Ya existe una institución con el mismo nombre.');
  });

  it('update stores the updated institution and clears submitting flag', async () => {
    const service = buildService();

    const { result } = renderHook(() =>
      useInstitutionMutation({ service }),
    );

    let updated: Institution | null = null;
    await act(async () => {
      updated = await result.current.update('abc-123', sampleInput);
    });

    expect(updated).toEqual(sample);
    expect(result.current.lastUpdated).toEqual(sample);
    expect(service.update).toHaveBeenCalledWith('abc-123', sampleInput);
  });

  it('remove returns true on success and tracks the deleted id', async () => {
    const onDeleted = vi.fn();
    const service = buildService();

    const { result } = renderHook(() =>
      useInstitutionMutation({ service, callbacks: { onDeleted } }),
    );

    let success: boolean | undefined;
    await act(async () => {
      success = await result.current.remove('abc-123');
    });

    expect(success).toBe(true);
    expect(onDeleted).toHaveBeenCalledWith('abc-123');
    expect(result.current.lastDeletedId).toBe('abc-123');
  });

  it('remove surfaces a NotFound error from the API', async () => {
    const service = buildService({
      remove: vi.fn(async () => {
        throw describeApiError('NotFound', 'Institución no encontrada.');
      }),
    });

    const { result } = renderHook(() =>
      useInstitutionMutation({ service }),
    );

    let success: boolean | undefined;
    await act(async () => {
      success = await result.current.remove('abc-123');
    });

    expect(success).toBe(false);
    expect(result.current.error).toBe('La institución solicitada no existe o ya fue eliminada.');
  });

  it('reset clears the previous state', async () => {
    const service = buildService();

    const { result } = renderHook(() =>
      useInstitutionMutation({ service }),
    );

    await act(async () => {
      await result.current.create(sampleInput);
    });

    expect(result.current.lastCreated).not.toBeNull();

    act(() => {
      result.current.reset();
    });

    expect(result.current.lastCreated).toBeNull();
    expect(result.current.lastUpdated).toBeNull();
    expect(result.current.lastDeletedId).toBeNull();
    expect(result.current.error).toBeNull();
  });
});
