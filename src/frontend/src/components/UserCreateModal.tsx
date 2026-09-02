'use client';

import { useEffect, useId, useMemo, useRef, useState } from 'react';
import { UserRole } from '../types/auth';
import type { UserInput } from '../types/user';

export interface UserCreateModalProps {
  readonly open: boolean;
  readonly submitting: boolean;
  readonly error: string | null;
  readonly onSubmit: (input: UserInput) => void;
  readonly onCancel: () => void;
}

interface FormState {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
  role: UserRole;
  isActive: boolean;
}

const EmptyForm: FormState = {
  username: '',
  email: '',
  password: '',
  confirmPassword: '',
  role: UserRole.User,
  isActive: true,
};

const PASSWORD_MIN_LENGTH = 8;

interface PasswordChecks {
  readonly minLength: boolean;
  readonly hasUppercase: boolean;
  readonly hasLowercase: boolean;
  readonly hasDigit: boolean;
  readonly hasSymbol: boolean;
}

function evaluatePassword(password: string): PasswordChecks {
  return {
    minLength: password.length >= PASSWORD_MIN_LENGTH,
    hasUppercase: /[A-Z]/.test(password),
    hasLowercase: /[a-z]/.test(password),
    hasDigit: /\d/.test(password),
    hasSymbol: /[^A-Za-z0-9]/.test(password),
  };
}

function isStrongEnough(checks: PasswordChecks): boolean {
  return (
    checks.minLength &&
    checks.hasUppercase &&
    checks.hasLowercase &&
    checks.hasDigit &&
    checks.hasSymbol
  );
}

export function UserCreateModal({
  open,
  submitting,
  error,
  onSubmit,
  onCancel,
}: UserCreateModalProps) {
  const titleId = useId();
  const usernameFieldRef = useRef<HTMLInputElement | null>(null);

  const [form, setForm] = useState<FormState>(EmptyForm);
  const [touched, setTouched] = useState<boolean>(false);

  useEffect(() => {
    if (!open) {
      return;
    }
    setForm(EmptyForm);
    setTouched(false);
  }, [open]);

  useEffect(() => {
    if (!open) {
      return;
    }
    const handleKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !submitting) {
        onCancel();
      }
    };
    document.addEventListener('keydown', handleKey);
    return () => {
      document.removeEventListener('keydown', handleKey);
    };
  }, [open, submitting, onCancel]);

  useEffect(() => {
    if (!open) {
      return;
    }
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    const focusTimer = window.setTimeout(() => {
      usernameFieldRef.current?.focus();
    }, 30);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.clearTimeout(focusTimer);
    };
  }, [open]);

  const passwordChecks = useMemo(() => evaluatePassword(form.password), [form.password]);
  const passwordsMatch = form.password === form.confirmPassword;
  const canSubmit =
    form.username.trim().length > 0 &&
    form.email.trim().length > 0 &&
    isStrongEnough(passwordChecks) &&
    passwordsMatch &&
    !submitting;

  if (!open) {
    return null;
  }

  const showMismatchError = touched && form.confirmPassword.length > 0 && !passwordsMatch;
  const showPasswordWeakError = touched && form.password.length > 0 && !isStrongEnough(passwordChecks);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setTouched(true);
    if (!canSubmit) {
      return;
    }
    onSubmit({
      username: form.username.trim(),
      email: form.email.trim(),
      password: form.password,
      role: form.role,
      isActive: form.isActive,
    });
  };

  return (
    <div
      className="modal-backdrop modal-backdrop--animated"
      role="presentation"
      onClick={(event) => {
        if (event.target === event.currentTarget && !submitting) {
          onCancel();
        }
      }}
    >
      <div
        className="modal modal--create-user"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        data-testid="users-create-modal"
      >
        <header className="modal__header modal__header--with-icon">
          <div className="modal__icon" aria-hidden="true">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth={1.8}
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
              <circle cx={9} cy={7} r={4} />
              <line x1={19} y1={8} x2={19} y2={14} />
              <line x1={22} y1={11} x2={16} y2={11} />
            </svg>
          </div>
          <div className="modal__title-group">
            <h2 id={titleId} className="modal__title">
              Nuevo usuario
            </h2>
            <p className="modal__subtitle">
              Registra una nueva cuenta y asigna su rol en el sistema.
            </p>
          </div>
          <button
            type="button"
            className="modal__close"
            onClick={onCancel}
            disabled={submitting}
            aria-label="Cerrar"
            data-testid="users-create-close"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth={2}
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <line x1={18} y1={6} x2={6} y2={18} />
              <line x1={6} y1={6} x2={18} y2={18} />
            </svg>
          </button>
        </header>
        <form onSubmit={handleSubmit} noValidate>
          <div className="modal__body modal__body--form">
            {error !== null && (
              <p role="alert" className="form__error" data-testid="users-create-error">
                {error}
              </p>
            )}

            <div className="form-grid">
              <label className="field">
                <span className="field__label">Usuario</span>
                <input
                  ref={usernameFieldRef}
                  type="text"
                  value={form.username}
                  onChange={(event) =>
                    setForm((previous) => ({ ...previous, username: event.target.value }))
                  }
                  className="field__input"
                  required
                  autoComplete="username"
                  placeholder="ej. maria.lopez"
                  data-testid="users-create-username"
                />
              </label>

              <label className="field">
                <span className="field__label">Correo electrónico</span>
                <input
                  type="email"
                  value={form.email}
                  onChange={(event) =>
                    setForm((previous) => ({ ...previous, email: event.target.value }))
                  }
                  className="field__input"
                  required
                  autoComplete="email"
                  placeholder="usuario@correo.com"
                  data-testid="users-create-email"
                />
              </label>
            </div>

            <label className="field">
              <span className="field__label">Rol</span>
              <select
                value={form.role}
                onChange={(event) =>
                  setForm((previous) => ({
                    ...previous,
                    role: event.target.value as UserRole,
                  }))
                }
                className="field__input"
                data-testid="users-create-role"
              >
                <option value={UserRole.User}>Usuario</option>
                <option value={UserRole.Analista}>Analista</option>
                <option value={UserRole.Solicitante}>Solicitante</option>
                <option value={UserRole.Admin}>Administrador</option>
              </select>
              <span className="field__hint">
                Define los permisos con los que la cuenta podrá operar.
              </span>
            </label>

            <div className="form-grid">
              <label className="field">
                <span className="field__label">Contraseña</span>
                <input
                  type="password"
                  value={form.password}
                  onChange={(event) =>
                    setForm((previous) => ({ ...previous, password: event.target.value }))
                  }
                  className="field__input"
                  required
                  autoComplete="new-password"
                  minLength={PASSWORD_MIN_LENGTH}
                  placeholder="Crea una contraseña segura"
                  data-testid="users-create-password"
                />
              </label>
              <label className="field">
                <span className="field__label">Confirmar contraseña</span>
                <input
                  type="password"
                  value={form.confirmPassword}
                  onChange={(event) =>
                    setForm((previous) => ({
                      ...previous,
                      confirmPassword: event.target.value,
                    }))
                  }
                  className="field__input"
                  required
                  autoComplete="new-password"
                  minLength={PASSWORD_MIN_LENGTH}
                  placeholder="Repite la contraseña"
                  data-testid="users-create-confirm-password"
                />
              </label>
            </div>

            <ul className="password-rules" aria-label="Requisitos de la contraseña">
              <li
                className={`password-rules__item${passwordChecks.minLength ? ' password-rules__item--ok' : ''}`}
              >
                <span className="password-rules__icon" aria-hidden="true">
                  {passwordChecks.minLength ? '✓' : '•'}
                </span>
                Mínimo {PASSWORD_MIN_LENGTH} caracteres
              </li>
              <li
                className={`password-rules__item${passwordChecks.hasUppercase ? ' password-rules__item--ok' : ''}`}
              >
                <span className="password-rules__icon" aria-hidden="true">
                  {passwordChecks.hasUppercase ? '✓' : '•'}
                </span>
                Una letra mayúscula
              </li>
              <li
                className={`password-rules__item${passwordChecks.hasLowercase ? ' password-rules__item--ok' : ''}`}
              >
                <span className="password-rules__icon" aria-hidden="true">
                  {passwordChecks.hasLowercase ? '✓' : '•'}
                </span>
                Una letra minúscula
              </li>
              <li
                className={`password-rules__item${passwordChecks.hasDigit ? ' password-rules__item--ok' : ''}`}
              >
                <span className="password-rules__icon" aria-hidden="true">
                  {passwordChecks.hasDigit ? '✓' : '•'}
                </span>
                Un número
              </li>
              <li
                className={`password-rules__item${passwordChecks.hasSymbol ? ' password-rules__item--ok' : ''}`}
              >
                <span className="password-rules__icon" aria-hidden="true">
                  {passwordChecks.hasSymbol ? '✓' : '•'}
                </span>
                Un símbolo
              </li>
              <li
                className={`password-rules__item${passwordsMatch ? ' password-rules__item--ok' : ''}`}
              >
                <span className="password-rules__icon" aria-hidden="true">
                  {passwordsMatch ? '✓' : '•'}
                </span>
                Las contraseñas coinciden
              </li>
            </ul>

            {(showMismatchError || showPasswordWeakError) && (
              <p role="alert" className="form__error form__error--inline">
                {showPasswordWeakError
                  ? 'La contraseña no cumple con todos los requisitos de seguridad.'
                  : 'Las contraseñas no coinciden.'}
              </p>
            )}

            <label className="field field--row">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(event) =>
                  setForm((previous) => ({
                    ...previous,
                    isActive: event.target.checked,
                  }))
                }
                data-testid="users-create-is-active"
              />
              <span className="field__label field__label--inline">
                Activar cuenta al crearla
                <span className="field__hint field__hint--inline">
                  El usuario podrá iniciar sesión inmediatamente.
                </span>
              </span>
            </label>
          </div>

          <footer className="modal__actions modal__actions--create-user">
            <button
              type="button"
              className="button button--ghost"
              onClick={onCancel}
              disabled={submitting}
              data-testid="users-create-cancel"
            >
              Cancelar
            </button>
            <button
              type="submit"
              className="button button--primary"
              disabled={!canSubmit}
              data-testid="users-create-submit"
            >
              {submitting ? (
                <>
                  <span className="button__spinner" aria-hidden="true" />
                  Creando...
                </>
              ) : (
                'Crear usuario'
              )}
            </button>
          </footer>
        </form>
      </div>
    </div>
  );
}
