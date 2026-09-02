import type { Metadata } from 'next';
import { AuditLogPage } from '../../../views/AuditLogPage';

export const metadata: Metadata = {
  title: 'Bitácora de auditoría — SB Client',
};

export default function AuditLogRoute() {
  return <AuditLogPage />;
}