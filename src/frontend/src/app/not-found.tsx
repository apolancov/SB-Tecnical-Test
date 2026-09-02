import Link from 'next/link';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Página no encontrada — SB Client',
};

export default function NotFound() {
  return (
    <main className="not-found">
      <h1>Página no encontrada</h1>
      <p>La página solicitada no existe.</p>
      <Link href="/dashboard" className="button">
        Ir al dashboard
      </Link>
    </main>
  );
}