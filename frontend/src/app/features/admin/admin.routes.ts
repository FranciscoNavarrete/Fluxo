import { Routes } from '@angular/router';
import { AdminShellComponent } from './shell/admin-shell.component';

export const adminRoutes: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    children: [
      { path: '', redirectTo: 'suscripciones', pathMatch: 'full' },
      {
        path: 'suscripciones',
        loadComponent: () =>
          import('./suscripciones/suscripciones-tabla.component').then(m => m.SuscripcionesTablaComponent),
      },
      {
        path: 'suscripciones/:id',
        loadComponent: () =>
          import('./suscripciones/detalle/suscripcion-detalle.component').then(m => m.SuscripcionDetalleComponent),
      },
      {
        path: 'planes',
        loadComponent: () =>
          import('./planes/planes.component').then(m => m.PlanesComponent),
      },
      {
        path: 'monitoreo',
        loadComponent: () =>
          import('./monitoreo/monitoreo.component').then(m => m.MonitoreoComponent),
      },
      {
        path: 'clientes/nuevo',
        loadComponent: () =>
          import('./clientes/crear-cliente.component').then(m => m.CrearClienteComponent),
      },
    ],
  },
];
