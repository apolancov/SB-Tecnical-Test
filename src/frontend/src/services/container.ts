import { ApiClient } from './api';
import { DefaultAuthenticationService, type AuthenticationService } from './authService';
import {
  DefaultAuditLogService,
  type AuditLogService,
} from './auditLogService';
import { DefaultCatalogService, type CatalogService } from './catalogService';
import { DefaultDashboardService, type DashboardService } from './dashboardService';
import { DefaultInstitutionService, type InstitutionService } from './institutionService';
import { DefaultRequestService, type RequestService } from './requestService';
import { DefaultUserService, type UserService } from './userService';
import {
  clearStoredAuthentication,
  readStoredAuthentication,
  type AuthenticationState,
} from './authStorage';

export interface ServiceContainer {
  readonly apiClient: ApiClient;
  readonly authenticationService: AuthenticationService;
  readonly institutionService: InstitutionService;
  readonly requestService: RequestService;
  readonly dashboardService: DashboardService;
  readonly catalogService: CatalogService;
  readonly auditLogService: AuditLogService;
  readonly userService: UserService;
  getAccessToken(): string | null;
  setAuthentication(state: AuthenticationState | null): void;
  clearAuthentication(): void;
}

interface BuildServiceContainerOptions {
  readonly initialAuthentication?: AuthenticationState | null;
  readonly navigateToLogin?: () => void;
}

export function buildServiceContainer(
  options: BuildServiceContainerOptions = {},
): ServiceContainer {
  let currentAuthentication: AuthenticationState | null =
    options.initialAuthentication ?? readStoredAuthentication();

  const apiClient = new ApiClient(
    () => currentAuthentication?.accessToken ?? null,
    () => {
      currentAuthentication = null;
      clearStoredAuthentication();
      if (options.navigateToLogin) {
        options.navigateToLogin();
      }
    },
  );

  const authenticationService = new DefaultAuthenticationService(apiClient);
  const institutionService = new DefaultInstitutionService(apiClient);
  const requestService = new DefaultRequestService(apiClient);
  const dashboardService = new DefaultDashboardService(apiClient);
  const catalogService = new DefaultCatalogService(apiClient);
  const auditLogService = new DefaultAuditLogService(apiClient);
  const userService = new DefaultUserService(apiClient);

  return {
    apiClient,
    authenticationService,
    institutionService,
    requestService,
    dashboardService,
    catalogService,
    auditLogService,
    userService,
    getAccessToken: () => currentAuthentication?.accessToken ?? null,
    setAuthentication: (state) => {
      currentAuthentication = state;
    },
    clearAuthentication: () => {
      currentAuthentication = null;
      clearStoredAuthentication();
    },
  };
}