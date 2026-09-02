'use client';

import type { FormEvent } from 'react';
import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Logo } from '../components/Logo';
import { useAuth } from '../hooks/useAuth';
import { isApiError, ApiErrorKind } from '../services/api';

function describeAuthenticationError(_message: string): string {
  return 'Usuario o contraseña inválidos.';
}

export function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const fromPath = searchParams?.get('from') ?? '/dashboard';

  const [username, setUsername] = useState<string>('');
  const [password, setPassword] = useState<string>('');
  const [submitting, setSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [validationMessage, setValidationMessage] = useState<string | null>(null);

  useEffect(() => {
    if (isAuthenticated) {
      router.replace(fromPath);
    }
  }, [isAuthenticated, router, fromPath]);

  if (isAuthenticated) {
    return (
      <div className="boot-loading" role="status" aria-live="polite">
        <div className="state state--loading">Cargando...</div>
      </div>
    );
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setValidationMessage(null);
    setErrorMessage(null);

    const trimmedUsername = username.trim();
    if (trimmedUsername.length === 0 || password.length === 0) {
      setValidationMessage('El usuario y la contraseña son obligatorios.');
      return;
    }

    setSubmitting(true);
    try {
      await login({ username: trimmedUsername, password });
      setPassword('');
      router.replace(fromPath);
    } catch (cause) {
      if (isApiError(cause) && cause.kind === ApiErrorKind.Unauthorized) {
        setErrorMessage(describeAuthenticationError(cause.message));
      } else if (isApiError(cause) && cause.kind === ApiErrorKind.Network) {
        setErrorMessage('El servicio no está disponible. Por favor, inténtelo de nuevo.');
      } else {
        setErrorMessage('No se pudo iniciar sesión. Por favor, inténtelo de nuevo.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <main className="login-page">
      <section className="card" aria-labelledby="login-title">
        <div className="login-card__brand">
          <Logo className="login-card__logo" size="large" alt="SB Client" />
          <h1 id="login-title" className="card__title">Iniciar sesión</h1>
          <p className="login-card__subtitle">
            Accede a la consola administrativa de instituciones.
          </p>
        </div>
        <form onSubmit={handleSubmit} className="form login-card" noValidate>
          <label className="field">
            <span className="field__label">Usuario</span>
            <input
              type="text"
              name="username"
              autoComplete="username"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              required
              aria-required="true"
              disabled={submitting}
              data-testid="username-input"
            />
          </label>
          <label className="field">
            <span className="field__label">Contraseña</span>
            <input
              type="password"
              name="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
              aria-required="true"
              disabled={submitting}
              data-testid="password-input"
            />
          </label>

          {validationMessage !== null && (
            <p role="alert" className="form__error" data-testid="validation-error">
              {validationMessage}
            </p>
          )}

          {errorMessage !== null && (
            <p role="alert" className="form__error" data-testid="auth-error">
              {errorMessage}
            </p>
          )}

          <button
            type="submit"
            className="button button--primary"
            disabled={submitting}
            data-testid="submit-button"
          >
            {submitting ? 'Iniciando sesión...' : 'Iniciar sesión'}
          </button>
        </form>
      </section>
    </main>
  );
}
