import { Injectable, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import {
  JwtClaims,
  LoginRequest,
  LoginResponse,
  RegistroRequest,
  Rol,
  UsuarioActual,
} from '../../shared/models';

const TOKEN_KEY = 'token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api    = inject(ApiService);
  private readonly router = inject(Router);

  private readonly _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));

  readonly token    = this._token.asReadonly();
  readonly claims   = computed(() => this.decodeClaims(this._token()));
  readonly usuario  = computed<UsuarioActual | null>(() => {
    const c = this.claims();
    if (!c) return null;
    return {
      userId:    c.userId,
      clienteId: c.clienteId,
      email:     c.email,
      nombre:    c.sub,
      role:      this.resolveRole(c),
    };
  });
  readonly isLoggedIn          = computed(() => !!this._token() && !this.isExpired());
  readonly isAdmin             = computed(() => this.usuario()?.role === 'ADMINISTRADOR');
  readonly isCliente           = computed(() => this.usuario()?.role === 'CLIENTE');
  readonly debeCambiarPassword = computed(() => this.claims()?.debeCambiarPassword === 'true');

  login(req: LoginRequest): Observable<LoginResponse> {
    return this.api.post<LoginResponse>('auth/login', req).pipe(
      tap(res => this.saveToken(res.token)),
    );
  }

  registro(req: RegistroRequest): Observable<LoginResponse> {
    return this.api.post<LoginResponse>('auth/registro', req).pipe(
      tap(res => this.saveToken(res.token)),
    );
  }

  obtenerPerfil(): Observable<{ nombre: string; apellido?: string; email: string; telefono?: string; clienteId: number }> {
    return this.api.get('cliente/perfil');
  }

  actualizarPerfil(datos: { nombre: string; apellido?: string; telefono?: string }): Observable<{ nombre: string; apellido?: string; email: string; telefono?: string }> {
    return this.api.put('cliente/perfil', datos);
  }

  cambiarPassword(nuevaPassword: string): Observable<LoginResponse> {
    return this.api.post<LoginResponse>('auth/cambiar-password', { nuevaPassword }).pipe(
      tap(res => this.saveToken(res.token)),
    );
  }

  solicitarReset(email: string): Observable<{ token: string }> {
    return this.api.post<{ token: string }>('auth/solicitar-reset', { email });
  }

  resetPassword(token: string, nuevaPassword: string): Observable<void> {
    return this.api.post<void>('auth/reset-password', { token, nuevaPassword });
  }

  // Permite cargar un token manualmente (dev/testing)
  setTokenDirecto(token: string): void {
    const claims = this.decodeClaims(token);
    if (!claims) throw new Error('Token inválido');
    this.saveToken(token);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this._token.set(null);
    this.router.navigate(['/auth/login']);
  }

  getRole(): Rol | null {
    const c = this.claims();
    if (!c) return null;
    return this.resolveRole(c);
  }

  private resolveRole(c: JwtClaims): Rol {
    return (
      c.role ??
      c['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
      'CLIENTE'
    ) as Rol;
  }

  getClienteId(): number | null {
    return this.claims()?.clienteId ?? null;
  }

  getUserId(): number | null {
    return this.claims()?.userId ?? null;
  }

  private saveToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
    this._token.set(token);
  }

  private decodeClaims(token: string | null): JwtClaims | null {
    if (!token) return null;
    try {
      return jwtDecode<JwtClaims>(token);
    } catch {
      return null;
    }
  }

  private isExpired(): boolean {
    const claims = this.decodeClaims(this._token());
    if (!claims) return true;
    return Date.now() >= claims.exp * 1000;
  }
}
