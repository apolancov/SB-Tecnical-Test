'use client';

import { useCallback, useEffect, useState } from 'react';
import { useService } from '../hooks/ServiceContext';
import { Pagination } from '../components/Pagination';
import { UserCreateModal } from '../components/UserCreateModal';
import { ApiErrorKind, isApiError } from '../services/api';
import { UserRole } from '../types/auth';
import type {
  User,
  UserInput,
  UserUpdateInput,
} from '../types/user';

interface FormState {
  username: string;
  email: string;
  password: string;
  role: UserRole;
  isActive: boolean;
}

const EmptyForm: FormState = {
  username: '',
  email: '',
  password: '',
  role: UserRole.User,
  isActive: true,
};

function describeError(error: unknown): string {
  if (isApiError(error)) {
    if (error.kind === ApiErrorKind.Forbidden) {
      return 'No tienes permiso para administrar usuarios.';
    }
    if (error.kind === ApiErrorKind.Unauthorized) {
      return 'Tu sesión ya no es válida. Por favor, inicia sesión nuevamente.';
    }
    if (error.kind === ApiErrorKind.NotFound) {
      return 'El usuario solicitado no existe o ya fue eliminado.';
    }
    if (error.kind === ApiErrorKind.Conflict) {
      return error.message || 'Conflicto al procesar la solicitud.';
    }
    if (error.kind === ApiErrorKind.Validation) {
      return error.message || 'Los datos enviados no son válidos.';
    }
    if (error.kind === ApiErrorKind.Network) {
      return 'El servicio no está disponible. Por favor, verifica tu conexión e inténtalo de nuevo.';
    }
  }
  return 'No se pudo completar la operación. Por favor, inténtalo de nuevo.';
}

export function UsersPage() {
  const { userService } = useService();

  const [users, setUsers] = useState<ReadonlyArray<User>>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState<number>(1);
  const [pageSize] = useState<number>(20);
  const [totalItems, setTotalItems] = useState<number>(0);
  const [totalPages, setTotalPages] = useState<number>(0);

  const [usernameFilter, setUsernameFilter] = useState<string>('');
  const [emailFilter, setEmailFilter] = useState<string>('');
  const [roleFilter, setRoleFilter] = useState<UserRole | ''>('');
  const [isActiveFilter, setIsActiveFilter] = useState<'' | 'true' | 'false'>('');

  const [showCreate, setShowCreate] = useState<boolean>(false);
  const [createSubmitting, setCreateSubmitting] = useState<boolean>(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const [editing, setEditing] = useState<User | null>(null);
  const [editForm, setEditForm] = useState<FormState>(EmptyForm);
  const [editSubmitting, setEditSubmitting] = useState<boolean>(false);
  const [editError, setEditError] = useState<string | null>(null);

  const [changingPasswordFor, setChangingPasswordFor] = useState<User | null>(null);
  const [newPassword, setNewPassword] = useState<string>('');
  const [passwordSubmitting, setPasswordSubmitting] = useState<boolean>(false);
  const [passwordError, setPasswordError] = useState<string | null>(null);

  const [deactivating, setDeactivating] = useState<User | null>(null);
  const [deactivateSubmitting, setDeactivateSubmitting] = useState<boolean>(false);
  const [deactivateError, setDeactivateError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await userService.search({
        page,
        pageSize,
        filters: {
          username: usernameFilter,
          email: emailFilter,
          role: roleFilter === '' ? null : roleFilter,
          isActive:
            isActiveFilter === ''
              ? null
              : isActiveFilter === 'true',
        },
      });
      setUsers(response.items);
      setTotalItems(response.totalItems);
      setTotalPages(response.totalPages);
    } catch (cause) {
      setError(describeError(cause));
      setUsers([]);
      setTotalItems(0);
      setTotalPages(0);
    } finally {
      setLoading(false);
    }
  }, [userService, page, pageSize, usernameFilter, emailFilter, roleFilter, isActiveFilter]);

  const runSearch = useCallback(() => {
    setPage(1);
    void refresh();
  }, [refresh]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const openCreate = useCallback(() => {
    setCreateError(null);
    setShowCreate(true);
  }, []);

  const closeCreate = useCallback(() => {
    if (createSubmitting) {
      return;
    }
    setShowCreate(false);
    setCreateError(null);
  }, [createSubmitting]);

  const submitCreate = useCallback(
    async (input: UserInput) => {
      setCreateSubmitting(true);
      setCreateError(null);
      try {
        await userService.create(input);
        setShowCreate(false);
        setPage(1);
        await refresh();
      } catch (cause) {
        setCreateError(describeError(cause));
      } finally {
        setCreateSubmitting(false);
      }
    },
    [userService, refresh],
  );

  const openEdit = useCallback((user: User) => {
    setEditing(user);
    setEditForm({
      username: user.username,
      email: user.email,
      password: '',
      role: user.role,
      isActive: user.isActive,
    });
    setEditError(null);
  }, []);

  const closeEdit = useCallback(() => {
    setEditing(null);
    setEditError(null);
  }, []);

  const submitEdit = useCallback(async () => {
    if (editing === null) {
      return;
    }
    setEditSubmitting(true);
    setEditError(null);
    try {
      const input: UserUpdateInput = {
        username: editForm.username.trim(),
        email: editForm.email.trim(),
        role: editForm.role,
        isActive: editForm.isActive,
      };
      await userService.update(editing.id, input);
      setEditing(null);
      await refresh();
    } catch (cause) {
      setEditError(describeError(cause));
    } finally {
      setEditSubmitting(false);
    }
  }, [userService, editing, editForm, refresh]);

  const openChangePassword = useCallback((user: User) => {
    setChangingPasswordFor(user);
    setNewPassword('');
    setPasswordError(null);
  }, []);

  const closeChangePassword = useCallback(() => {
    setChangingPasswordFor(null);
    setPasswordError(null);
    setNewPassword('');
  }, []);

  const submitChangePassword = useCallback(async () => {
    if (changingPasswordFor === null) {
      return;
    }
    setPasswordSubmitting(true);
    setPasswordError(null);
    try {
      await userService.changePassword(changingPasswordFor.id, newPassword);
      setChangingPasswordFor(null);
      setNewPassword('');
    } catch (cause) {
      setPasswordError(describeError(cause));
    } finally {
      setPasswordSubmitting(false);
    }
  }, [userService, changingPasswordFor, newPassword]);

  const openDeactivate = useCallback((user: User) => {
    setDeactivating(user);
    setDeactivateError(null);
  }, []);

  const closeDeactivate = useCallback(() => {
    setDeactivating(null);
    setDeactivateError(null);
  }, []);

  const submitDeactivate = useCallback(async () => {
    if (deactivating === null) {
      return;
    }
    setDeactivateSubmitting(true);
    setDeactivateError(null);
    try {
      await userService.remove(deactivating.id);
      setDeactivating(null);
      await refresh();
    } catch (cause) {
      setDeactivateError(describeError(cause));
    } finally {
      setDeactivateSubmitting(false);
    }
  }, [userService, deactivating, refresh]);

  return (
    <section className="page" data-testid="users-page">
      <header className="page__header">
        <div className="page__header-row">
          <div>
            <h1 className="page__title">Usuarios</h1>
            <p className="page__subtitle">
              {loading
                ? 'Cargando resultados...'
                : `${totalItems} resultado${totalItems === 1 ? '' : 's'} encontrado${totalItems === 1 ? '' : 's'}.`}
            </p>
          </div>
          <button
            type="button"
            className="button button--primary"
            onClick={openCreate}
            data-testid="users-new-button"
          >
            Nuevo usuario
          </button>
        </div>
      </header>

      <form
        className="filters"
        onSubmit={(event) => {
          event.preventDefault();
          runSearch();
        }}
      >
        <label className="field">
          <span className="field__label">Usuario</span>
          <input
            type="text"
            value={usernameFilter}
            onChange={(event) => setUsernameFilter(event.target.value)}
            className="field__input"
            data-testid="users-filter-username"
          />
        </label>
        <label className="field">
          <span className="field__label">Correo</span>
          <input
            type="text"
            value={emailFilter}
            onChange={(event) => setEmailFilter(event.target.value)}
            className="field__input"
            data-testid="users-filter-email"
          />
        </label>
        <label className="field">
          <span className="field__label">Rol</span>
          <select
            value={roleFilter}
            onChange={(event) =>
              setRoleFilter(event.target.value === '' ? '' : (event.target.value as UserRole))
            }
            className="field__input"
            data-testid="users-filter-role"
          >
            <option value="">Todos</option>
            <option value={UserRole.Admin}>Administrador</option>
            <option value={UserRole.User}>Usuario</option>
            <option value={UserRole.Analista}>Analista</option>
            <option value={UserRole.Solicitante}>Solicitante</option>
          </select>
        </label>
        <label className="field">
          <span className="field__label">Estado</span>
          <select
            value={isActiveFilter}
            onChange={(event) =>
              setIsActiveFilter(
                event.target.value === ''
                  ? ''
                  : (event.target.value as 'true' | 'false'),
              )
            }
            className="field__input"
            data-testid="users-filter-is-active"
          >
            <option value="">Todos</option>
            <option value="true">Activos</option>
            <option value="false">Inactivos</option>
          </select>
        </label>
        <div className="filters__actions">
          <button
            type="submit"
            className="button button--small"
            disabled={loading}
            data-testid="users-filter-submit"
          >
            Aplicar filtros
          </button>
          <button
            type="button"
            className="button button--small button--ghost"
            disabled={loading}
            onClick={() => {
              setUsernameFilter('');
              setEmailFilter('');
              setRoleFilter('');
              setIsActiveFilter('');
            }}
            data-testid="users-filter-reset"
          >
            Restablecer
          </button>
          <button
            type="button"
            className="button button--small button--ghost"
            disabled={loading}
            onClick={refresh}
            data-testid="users-refresh"
          >
            Actualizar
          </button>
        </div>
      </form>

      {error !== null && (
        <div role="alert" className="state state--error">
          <p>{error}</p>
        </div>
      )}

      {loading ? (
        <div role="status" className="state state--loading">
          Cargando usuarios...
        </div>
      ) : users.length === 0 ? (
        <div role="status" className="state state--empty" data-testid="users-empty">
          No hay usuarios que coincidan con los filtros aplicados.
        </div>
      ) : (
        <table className="request-table" aria-label="Usuarios" data-testid="users-table">
          <thead>
            <tr>
              <th scope="col">Usuario</th>
              <th scope="col">Correo</th>
              <th scope="col">Rol</th>
              <th scope="col">Estado</th>
              <th scope="col">Creado</th>
              <th scope="col">Acciones</th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id} data-testid={`users-row-${user.id}`}>
                <td>{user.username}</td>
                <td>{user.email}</td>
                <td>{user.role}</td>
                <td>
                  <span
                    className={`badge ${user.isActive ? 'badge--status-Resolved' : 'badge--status-Rejected'}`}
                  >
                    {user.isActive ? 'Activo' : 'Inactivo'}
                  </span>
                </td>
                <td>{user.createdAt}</td>
                <td>
                  <button
                    type="button"
                    className="button button--small"
                    onClick={() => openEdit(user)}
                    data-testid={`users-edit-${user.id}`}
                  >
                    Editar
                  </button>
                  <button
                    type="button"
                    className="button button--small"
                    onClick={() => openChangePassword(user)}
                    data-testid={`users-password-${user.id}`}
                  >
                    Contraseña
                  </button>
                  {user.isActive ? (
                    <button
                      type="button"
                      className="button button--small button--danger"
                      onClick={() => openDeactivate(user)}
                      data-testid={`users-deactivate-${user.id}`}
                    >
                      Desactivar
                    </button>
                  ) : null}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <Pagination page={page} totalPages={totalPages} onChange={setPage} />

      <UserCreateModal
        open={showCreate}
        submitting={createSubmitting}
        error={createError}
        onSubmit={submitCreate}
        onCancel={closeCreate}
      />

      {editing !== null && (
        <div role="dialog" aria-modal="true" className="modal" data-testid="users-edit-modal">
          <div className="modal__panel">
            <h2 className="modal__title">Editar usuario</h2>
            {editError !== null && (
              <div role="alert" className="state state--error">
                <p>{editError}</p>
              </div>
            )}
            <form
              onSubmit={(event) => {
                event.preventDefault();
                void submitEdit();
              }}
            >
              <label className="field">
                <span className="field__label">Usuario</span>
                <input
                  type="text"
                  value={editForm.username}
                  onChange={(event) =>
                    setEditForm((form) => ({ ...form, username: event.target.value }))
                  }
                  className="field__input"
                  required
                  data-testid="users-edit-username"
                />
              </label>
              <label className="field">
                <span className="field__label">Correo</span>
                <input
                  type="email"
                  value={editForm.email}
                  onChange={(event) =>
                    setEditForm((form) => ({ ...form, email: event.target.value }))
                  }
                  className="field__input"
                  required
                  data-testid="users-edit-email"
                />
              </label>
              <label className="field">
                <span className="field__label">Rol</span>
                <select
                  value={editForm.role}
                  onChange={(event) =>
                    setEditForm((form) => ({
                      ...form,
                      role: event.target.value as UserRole,
                    }))
                  }
                  className="field__input"
                  data-testid="users-edit-role"
                >
                  <option value={UserRole.Admin}>Administrador</option>
                  <option value={UserRole.User}>Usuario</option>
                  <option value={UserRole.Analista}>Analista</option>
                  <option value={UserRole.Solicitante}>Solicitante</option>
                </select>
              </label>
              <label className="field field--checkbox">
                <input
                  type="checkbox"
                  checked={editForm.isActive}
                  onChange={(event) =>
                    setEditForm((form) => ({
                      ...form,
                      isActive: event.target.checked,
                    }))
                  }
                  data-testid="users-edit-is-active"
                />
                <span className="field__label">Activo</span>
              </label>
              <div className="modal__actions">
                <button
                  type="button"
                  className="button button--ghost"
                  onClick={closeEdit}
                  disabled={editSubmitting}
                  data-testid="users-edit-cancel"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  className="button button--primary"
                  disabled={editSubmitting}
                  data-testid="users-edit-submit"
                >
                  {editSubmitting ? 'Guardando...' : 'Guardar'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {changingPasswordFor !== null && (
        <div
          role="dialog"
          aria-modal="true"
          className="modal"
          data-testid="users-password-modal"
        >
          <div className="modal__panel">
            <h2 className="modal__title">
              Cambiar contraseña de {changingPasswordFor.username}
            </h2>
            {passwordError !== null && (
              <div role="alert" className="state state--error">
                <p>{passwordError}</p>
              </div>
            )}
            <form
              onSubmit={(event) => {
                event.preventDefault();
                void submitChangePassword();
              }}
            >
              <label className="field">
                <span className="field__label">Nueva contraseña</span>
                <input
                  type="password"
                  value={newPassword}
                  onChange={(event) => setNewPassword(event.target.value)}
                  className="field__input"
                  required
                  minLength={8}
                  data-testid="users-password-input"
                />
                <span className="field__hint">Mínimo 8 caracteres.</span>
              </label>
              <div className="modal__actions">
                <button
                  type="button"
                  className="button button--ghost"
                  onClick={closeChangePassword}
                  disabled={passwordSubmitting}
                  data-testid="users-password-cancel"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  className="button button--primary"
                  disabled={passwordSubmitting}
                  data-testid="users-password-submit"
                >
                  {passwordSubmitting ? 'Cambiando...' : 'Cambiar'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {deactivating !== null && (
        <div role="dialog" aria-modal="true" className="modal" data-testid="users-deactivate-modal">
          <div className="modal__panel">
            <h2 className="modal__title">Desactivar usuario</h2>
            <p>
              ¿Desactivar al usuario <strong>{deactivating.username}</strong>?
              El usuario no podrá iniciar sesión pero sus datos permanecerán en el sistema.
            </p>
            {deactivateError !== null && (
              <div role="alert" className="state state--error">
                <p>{deactivateError}</p>
              </div>
            )}
            <div className="modal__actions">
              <button
                type="button"
                className="button button--ghost"
                onClick={closeDeactivate}
                disabled={deactivateSubmitting}
                data-testid="users-deactivate-cancel"
              >
                Cancelar
              </button>
              <button
                type="button"
                className="button button--danger"
                onClick={() => void submitDeactivate()}
                disabled={deactivateSubmitting}
                data-testid="users-deactivate-confirm"
              >
                {deactivateSubmitting ? 'Desactivando...' : 'Desactivar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
