'use client';

import type { FormEvent } from 'react';
import { useState } from 'react';
import { CommentVisibility, type CommentVisibility as CommentVisibilityType } from '../types/request';
import { commentVisibilityOptions } from './RequestBadges';

export interface CommentFormErrors {
  readonly text?: string;
  readonly visibility?: string;
}

export interface CommentFormProps {
  readonly defaultVisibility?: CommentVisibilityType;
  readonly allowInternal: boolean;
  readonly submitting: boolean;
  readonly errors?: CommentFormErrors;
  readonly disabled?: boolean;
  readonly onSubmit: (input: {
    readonly text: string;
    readonly visibility: CommentVisibilityType;
  }) => void;
}

export function CommentForm({
  defaultVisibility = CommentVisibility.Requester,
  allowInternal,
  submitting,
  errors,
  disabled = false,
  onSubmit,
}: CommentFormProps) {
  const [text, setText] = useState<string>('');
  const [visibility, setVisibility] = useState<CommentVisibilityType>(
    allowInternal ? defaultVisibility : CommentVisibility.Requester,
  );

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmed = text.trim();
    if (trimmed.length === 0) {
      return;
    }
    onSubmit({ text: trimmed, visibility });
  };

  const showError = (field: keyof CommentFormErrors): string | undefined => errors?.[field];

  return (
    <form
      className="comment-form"
      aria-label="Agregar comentario"
      onSubmit={handleSubmit}
    >
      <label className="field">
        <span className="field__label">Comentario</span>
        <textarea
          value={text}
          onChange={(event) => setText(event.target.value)}
          disabled={disabled || submitting}
          data-testid="comment-form-text"
          maxLength={4096}
        />
        {showError('text') && (
          <span className="field__error" role="alert" data-testid="comment-form-text-error">
            {showError('text')}
          </span>
        )}
      </label>
      <div className="comment-form__options">
        <span className="field__label">Visibilidad</span>
        {allowInternal ? (
          commentVisibilityOptions().map((option) => (
            <label key={option.value}>
              <input
                type="radio"
                name="comment-visibility"
                value={option.value}
                checked={visibility === option.value}
                onChange={() => setVisibility(option.value)}
                disabled={disabled || submitting}
                data-testid={`comment-form-visibility-${option.value}`}
              />
              {option.name}
            </label>
          ))
        ) : (
          <span data-testid="comment-form-visibility-Requester">Público</span>
        )}
      </div>
      <div>
        <button
          type="submit"
          className="button button--primary button--small"
          disabled={disabled || submitting || text.trim().length === 0}
          data-testid="comment-form-submit"
        >
          {submitting ? 'Enviando...' : 'Agregar comentario'}
        </button>
      </div>
    </form>
  );
}