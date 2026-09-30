import { Routes } from '@angular/router';
import { ClienteShellComponent } from './shell/cliente-shell.component';

export const clienteRoutes: Routes = [
  {
    path: '',
    component: ClienteShellComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./dashboard/dashboard.component').then(m => m.DashboardComponent),
      },
      {
        path: 'historial-pagos',
        loadComponent: () =>
          import('./historial-pagos/historial-pagos.component').then(m => m.HistorialPagosComponent),
      },
      {
        path: 'pago-unico',
        loadComponent: () =>
          import('./pago-unico/pago-unico.component').then(m => m.PagoUnicoComponent),
      },
      {
        path: 'perfil',
        loadComponent: () =>
          import('./perfil/perfil.component').then(m => m.PerfilComponent),
      },
      {
        path: 'cambiar-password',
        loadComponent: () =>
          import('./cambiar-password/cambiar-password.component').then(m => m.CambiarPasswordComponent),
      },
    ],
  },
];
