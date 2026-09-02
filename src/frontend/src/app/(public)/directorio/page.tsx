import type { Metadata } from 'next';
import { PublicInstitutionsPage } from '../../../views/PublicInstitutionsPage';

export const metadata: Metadata = {
  title: 'Directorio de instituciones — SB Client',
  description:
    'Catálogo público de instituciones gubernamentales. Consulta, filtra y explora sin necesidad de iniciar sesión.',
};

export default function PublicInstitutionsRoute() {
  return <PublicInstitutionsPage />;
}
