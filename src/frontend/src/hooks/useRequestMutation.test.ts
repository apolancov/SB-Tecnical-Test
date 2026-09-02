import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, renderHook, waitFor } from '@testing-library/react';
import { useRequestMutation } from '../hooks/useRequestMutation';
import {
  CommentVisibility,
  RequestPriority,
  RequestStatus,
} from '../types/request';
import type { DefaultRequestService } from '../services/requestService';

describe('useRequestMutation', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
  });

  function buildService(): DefaultRequestService {
    return {
      create: fetchMock,
      update: fetchMock,
      changeStatus: fetchMock,
      assign: fetchMock,
      reopen: fetchMock,
      addComment: fetchMock,
      search: fetchMock,
      getById: fetchMock,
    } as unknown as DefaultRequestService;
  }

  it('captures a successful create', async () => {
    const created = {
      id: 'r1',
      code: 'SOL-2026-0001',
      title: 't',
      description: 'd',
      status: RequestStatus.Submitted,
      priority: RequestPriority.Medium,
      createdAt: '2026-09-01T10:00:00Z',
      dueDate: null,
      evidenceUrl: null,
      closedAt: null,
      areaId: 'a',
      area: 'M',
      requestTypeId: 't',
      requestType: 'I',
      requesterId: 'r',
      requesterUsername: 'j',
      requesterEmail: 'j@e.local',
      responsibleId: null,
      responsibleUsername: null,
      responsibleEmail: null,
    };
    fetchMock.mockResolvedValue(created);

    const service = buildService();
    const { result } = renderHook(() => useRequestMutation({ service }));

    let returned: unknown = null;
    await act(async () => {
      const promise = result.current.create({
        title: 't',
        description: 'd',
        priority: RequestPriority.Medium,
        areaId: 'a',
        requestTypeId: 't',
        dueDate: null,
        evidenceUrl: null,
      });
      returned = await promise;
    });

    expect(returned).toEqual(created);
    await waitFor(() => {
      expect(result.current.lastCreated).toEqual(created);
    });
    expect(result.current.submitting).toBe(false);
  });

  it('captures thrown errors', async () => {
    fetchMock.mockRejectedValue(new Error('boom'));

    const service = buildService();
    const { result } = renderHook(() => useRequestMutation({ service }));

    await act(async () => {
      await result.current.changeStatus('r1', {
        newStatus: RequestStatus.InReview,
        comment: 'c',
      });
    });

    expect(result.current.error).toMatch(/inténtalo de nuevo/i);
    expect(result.current.submitting).toBe(false);
  });

  it('adds comments and stores the result', async () => {
    const comment = {
      id: 'c1',
      text: 'ok',
      visibility: CommentVisibility.Requester,
      date: '2026-09-01T12:00:00Z',
      authorId: 'u',
      authorUsername: 'juan',
    };
    fetchMock.mockResolvedValue(comment);

    const service = buildService();
    const { result } = renderHook(() => useRequestMutation({ service }));

    await act(async () => {
      await result.current.addComment('r1', {
        text: 'ok',
        visibility: CommentVisibility.Requester,
      });
    });

    await waitFor(() => {
      expect(result.current.lastComment).toEqual(comment);
    });
  });
});