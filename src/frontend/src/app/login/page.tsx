import type { Metadata } from 'next';
import { LoginPage } from '../../views/LoginPage';

export const metadata: Metadata = {
  title: 'Iniciar sesión — SB Client',
};

export default function LoginRoute() {
  return <LoginPage />;
}
