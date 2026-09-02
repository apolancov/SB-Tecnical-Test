'use client';

import type { ReactNode } from 'react';
import {
  CommentVisibility as CommentVisibilityEnum,
  RequestPriority as RequestPriorityEnum,
  RequestStatus as RequestStatusEnum,
  describeCommentVisibility,
  describeRequestPriority,
  describeRequestStatus,
  type CommentVisibility,
  type RequestPriority,
  type RequestStatus,
} from '../types/request';

export interface RequestStatusBadgeProps {
  readonly status: RequestStatus;
}

export function RequestStatusBadge({ status }: RequestStatusBadgeProps) {
  return (
    <span
      className={`badge badge--status-${status}`}
      data-testid={`status-badge-${status}`}
      aria-label={`Estado: ${describeRequestStatus(status)}`}
    >
      {describeRequestStatus(status)}
    </span>
  );
}

export function requestStatusOptions(): ReadonlyArray<{ value: RequestStatus; name: string }> {
  return Object.values(RequestStatusEnum).map((value) => ({
    value,
    name: describeRequestStatus(value),
  }));
}

export interface RequestPriorityBadgeProps {
  readonly priority: RequestPriority;
}

export function RequestPriorityBadge({ priority }: RequestPriorityBadgeProps) {
  return (
    <span
      className={`badge badge--priority-${priority}`}
      data-testid={`priority-badge-${priority}`}
      aria-label={`Prioridad: ${describeRequestPriority(priority)}`}
    >
      {describeRequestPriority(priority)}
    </span>
  );
}

export function requestPriorityOptions(): ReadonlyArray<{ value: RequestPriority; name: string }> {
  return Object.values(RequestPriorityEnum).map((value) => ({
    value,
    name: describeRequestPriority(value),
  }));
}

export interface CommentVisibilityBadgeProps {
  readonly visibility: CommentVisibility;
}

export function CommentVisibilityBadge({ visibility }: CommentVisibilityBadgeProps) {
  return (
    <span
      className={`badge badge--visibility-${visibility}`}
      data-testid={`comment-visibility-${visibility}`}
      aria-label={`Visibilidad: ${describeCommentVisibility(visibility)}`}
    >
      {describeCommentVisibility(visibility)}
    </span>
  );
}

export interface BadgeProps {
  readonly children: ReactNode;
  readonly tone?: 'neutral';
}

export function Badge({ children }: BadgeProps) {
  return <span className="badge">{children}</span>;
}

export function commentVisibilityOptions(): ReadonlyArray<{ value: CommentVisibility; name: string }> {
  return Object.values(CommentVisibilityEnum).map((value) => ({
    value,
    name: describeCommentVisibility(value),
  }));
}