import type { Metadata } from 'next';
import { UsersPage } from '../../../views/UsersPage';

export const metadata: Metadata = {
  title: 'Usuarios — SB Client',
};

export default function UsersRoute() {
  return <UsersPage />;
}