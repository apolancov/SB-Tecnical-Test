'use client';

import { useState } from 'react';
import type { RequestComment } from '../types/request';
import { CommentVisibility } from '../types/request';
import { CommentForm } from './CommentForm';
import { CommentVisibilityBadge } from './RequestBadges';
import { formatDateTime } from './dateFormat';

export interface RequestCommentsProps {
  readonly comments: ReadonlyArray<RequestComment>;
  readonly allowInternal: boolean;
  readonly submitting: boolean;
  readonly error: string | null;
  readonly onAdd: (input: { text: string; visibility: import('../types/request').CommentVisibility }) => void;
}

type CommentFilter = 'all' | 'public' | 'internal';

export function RequestComments({
  comments,
  allowInternal,
  submitting,
  error,
  onAdd,
}: RequestCommentsProps) {
  const [filter, setFilter] = useState<CommentFilter>('all');

  const visibleComments = comments.filter((comment) => {
    if (filter === 'all') {
      return true;
    }
    if (filter === 'internal') {
      return comment.visibility === CommentVisibility.Internal;
    }
    return comment.visibility === CommentVisibility.Requester;
  });

  return (
    <section aria-label="Comentarios" data-testid="request-comments">
      {allowInternal && (
        <div className="tabs" role="tablist" aria-label="Filtro de comentarios">
          <button
            type="button"
            role="tab"
            aria-selected={filter === 'all'}
            className="tab"
            onClick={() => setFilter('all')}
            data-testid="request-comments-tab-all"
          >
            Todos ({comments.length})
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={filter === 'public'}
            className="tab"
            onClick={() => setFilter('public')}
            data-testid="request-comments-tab-public"
          >
            Públicos ({comments.filter((c) => c.visibility === CommentVisibility.Requester).length})
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={filter === 'internal'}
            className="tab"
            onClick={() => setFilter('internal')}
            data-testid="request-comments-tab-internal"
          >
            Internos ({comments.filter((c) => c.visibility === CommentVisibility.Internal).length})
          </button>
        </div>
      )}

      {visibleComments.length === 0 ? (
        <p className="field__hint" data-testid="request-comments-empty">
          No hay comentarios {filter === 'all' ? '' : filter === 'internal' ? 'internos' : 'públicos'} para mostrar.
        </p>
      ) : (
        <ul className="comment-list" data-testid="request-comments-list">
          {visibleComments.map((comment) => (
            <li
              key={comment.id}
              className={
                comment.visibility === CommentVisibility.Internal
                  ? 'comment comment--internal'
                  : 'comment'
              }
              data-testid={`request-comment-${comment.id}`}
            >
              <div className="comment__head">
                <span className="comment__author">{comment.authorUsername}</span>
                <CommentVisibilityBadge visibility={comment.visibility} />
                <span className="comment__date">{formatDateTime(comment.date)}</span>
              </div>
              <p className="comment__text">{comment.text}</p>
            </li>
          ))}
        </ul>
      )}

      <h3 className="request-detail__section-title">Agregar comentario</h3>
      <CommentForm
        allowInternal={allowInternal}
        submitting={submitting}
        onSubmit={onAdd}
      />
      {error !== null && (
        <p role="alert" className="form__error" data-testid="request-comments-error">
          {error}
        </p>
      )}
    </section>
  );
}