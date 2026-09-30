import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { Rol } from '../../shared/models';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  const rolesPermitidos: Rol[] = route.data['roles'] ?? [];
  const rolUsuario = auth.getRole();

  if (rolUsuario && rolesPermitidos.includes(rolUsuario)) return true;

  // Admin intenta entrar a cliente → manda al admin
  if (rolUsuario === 'ADMINISTRADOR' || rolUsuario === 'SISTEMA') {
    router.navigate(['/admin']);
    return false;
  }

  router.navigate(['/auth/login']);
  return false;
};
