import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'admin', pathMatch: 'full' },

  // Auth (público)
  {
    path: 'auth',
    loadChildren: () =>
      import('./features/auth/auth.routes').then(m => m.authRoutes),
  },

  // Cliente — solo rol CLIENTE (admin queda en /admin)
  {
    path: 'cliente',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['CLIENTE'] },
    loadChildren: () =>
      import('./features/cliente/cliente.routes').then(m => m.clienteRoutes),
  },

  // Admin — solo ADMINISTRADOR o SISTEMA
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['ADMINISTRADOR', 'SISTEMA'] },
    loadChildren: () =>
      import('./features/admin/admin.routes').then(m => m.adminRoutes),
  },

  // Callbacks Mercado Pago
  {
    path: 'suscripciones/resultado',
    loadComponent: () =>
      import('./features/suscripciones-resultado/suscripciones-resultado.component')
        .then(m => m.SuscripcionesResultadoComponent),
  },
  {
    path: 'pagos-unicos/resultado',
    loadComponent: () =>
      import('./features/pagos-unicos-resultado/pagos-unicos-resultado.component')
        .then(m => m.PagosUnicosResultadoComponent),
  },

  { path: '**', redirectTo: 'cliente' },
];
