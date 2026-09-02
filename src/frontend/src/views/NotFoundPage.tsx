'use client';

import Link from 'next/link';

export function NotFoundPage() {
  return (
    <main className="not-found">
      <h1>Página no encontrada</h1>
      <p>La página solicitada no existe.</p>
      <Link href="/institutions" className="button">
        Ir a instituciones
      </Link>
    </main>
  );
}
