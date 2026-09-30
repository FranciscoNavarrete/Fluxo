import { Routes } from '@angular/router';
import { AuthShellComponent } from './shell/auth-shell.component';

export const authRoutes: Routes = [
  {
    path: '',
    component: AuthShellComponent,
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      {
        path: 'login',
        loadComponent: () =>
          import('./login/login.component').then(m => m.LoginComponent),
      },
      {
        path: 'registro',
        loadComponent: () =>
          import('./registro/registro.component').then(m => m.RegistroComponent),
      },
      {
        path: 'recuperar',
        loadComponent: () =>
          import('./recuperar/recuperar.component').then(m => m.RecuperarComponent),
      },
      {
        path: 'reset-password',
        loadComponent: () =>
          import('./reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
      },
    ],
  },
];
