interface RuntimeConfiguration {
  readonly apiBaseUrl: string;
}

const DefaultApiBaseUrl = 'http://localhost:5180';

function readApiBaseUrl(): string {
  const fromEnvironment = process.env.NEXT_PUBLIC_API_URL;
  if (typeof fromEnvironment === 'string' && fromEnvironment.trim().length > 0) {
    return fromEnvironment.trim().replace(/\/+$/, '');
  }
  return DefaultApiBaseUrl;
}

export const runtimeConfiguration: RuntimeConfiguration = {
  apiBaseUrl: readApiBaseUrl(),
};
