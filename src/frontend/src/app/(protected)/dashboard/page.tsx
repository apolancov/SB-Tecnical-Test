import type { Metadata } from 'next';
import { DashboardPage } from '../../../views/DashboardPage';

export const metadata: Metadata = {
  title: 'Dashboard — SB Client',
};

export default function DashboardRoute() {
  return <DashboardPage />;
}