export type Rol = 'ADMINISTRADOR' | 'SISTEMA' | 'CLIENTE';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiracion: string;
}

export interface JwtClaims {
  sub: string;
  email: string;
  userId: number;
  clienteId?: number;
  role?: Rol;
  // Claim larga que genera JwtSecurityTokenHandler de .NET cuando usa ClaimTypes.Role
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: Rol;
  debeCambiarPassword?: string; // viene como string "true"/"false" desde el JWT
  exp: number;
  iat?: number;
  nbf?: number;
}

export interface RegistroRequest {
  nombre: string;
  apellido: string;
  email: string;
  password: string;
  telefono?: string;
  dni?: string;
}

export interface UsuarioActual {
  userId: number;
  clienteId?: number;
  email: string;
  nombre: string;
  role: Rol;
}
