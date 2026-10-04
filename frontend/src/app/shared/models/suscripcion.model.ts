// ─── Paginación del backend ───────────────────────────────────────────────────
// El backend devuelve { listaResultado: T[], totalFilas: number }

export interface BackendPaginado<T> {
  listaResultado: T[];
  totalFilas: number;
}

// ─── Planes ──────────────────────────────────────────────────────────────────

export interface MpPlan {
  mpPlanId: number;
  nombre: string;
  descripcion?: string;
  monto: number;
  moneda: string;
  tipoFrecuencia: string;   // 'months' | 'days'
  frecuencia: number;
  diasGratis: number;
  repeticiones?: number | null;
  mpPlanExternoId?: string;
  activo: boolean;
  fechaHoraCreacion: string;
}

export interface CrearPlanRequest {
  nombre: string;
  descripcion?: string;
  monto: number;
  moneda: string;
  tipoFrecuencia: string;
  frecuencia: number;
  diasGratis: number;
  repeticiones?: number | null;
}

export interface EditarPlanRequest {
  mpPlanId: number;
  nombre: string;
  descripcion?: string;
  monto: number;
  moneda: string;
  tipoFrecuencia: string;
  frecuencia: number;
  diasGratis: number;
  repeticiones?: number | null;
  activo: boolean;
}

// ─── Suscripciones ───────────────────────────────────────────────────────────

export type EstadoSuscripcion =
  | 'pending'
  | 'authorized'
  | 'paused'
  | 'suspended'
  | 'cancelled';

export interface MpSuscripcion {
  mpSuscripcionId: number;
  clienteId: number;
  clienteNombre: string;
  clienteEmail: string;
  mpPlanId: number;
  planNombre: string;
  planMonto: number;
  planMoneda: string;
  tipoFrecuencia: string;
  frecuencia: number;
  gatewaySuscripcionId: string;
  gatewayProveedor: string;
  mpPayerId?: string;
  initPoint?: string;
  estado: EstadoSuscripcion;
  fechaInicio: string;
  proximoCobro: string | null;
  ultimoCobro: string | null;
  fechaSuspension: string | null;
  fechaCancelacion: string | null;
  motivoCancelacion?: string;
  intentosReintento: number;
  maxReintentos: number;
  terminosVersion?: string;
  fechaHoraCreacion: string;
}

export interface CrearSuscripcionRequest {
  clienteId: number;
  mpPlanId: number;
  payerEmail: string;
  diaCobro?: number;
  backUrl?: string;
  terminosVersion?: string;
}

export interface CrearSuscripcionResponse {
  mpSuscripcionId: number;
  gatewaySuscripcionId: string;
  initPoint: string;
  estado: string;
}

export interface CancelarSuscripcionRequest {
  mpSuscripcionId: number;
  motivo?: string;
}

// ─── Transacciones (pagos) ───────────────────────────────────────────────────

export type EstadoPago = 'approved' | 'rejected' | 'pending' | 'cancelled' | 'refunded';

export interface MpTransaccion {
  mpTransaccionId: number;
  mpSuscripcionId: number;
  gatewaySuscripcionId: string;
  clienteId: number;
  clienteNombre: string;
  clienteEmail: string;
  gatewayPagoId: string;
  monto: number;
  moneda: string;
  estado: EstadoPago;
  estadoDetalle?: string;
  numeroIntento: number;
  fechaProcesado: string;
}

// ─── Dunning ─────────────────────────────────────────────────────────────────

export interface DunningLog {
  mpDunningLogId: number;
  mpSuscripcionId: number;
  accion: string;
  diasVencido: number;
  canal: string;
  fechaEjecucion: string;
}

export interface DunningResponse {
  mensaje: string;
}

// ─── Resultado de pago (callback MP) ─────────────────────────────────────────

export interface ResultadoPagoParams {
  status: 'approved' | 'pending' | 'failure' | string;
  payment_id: string;
  external_reference: string;
  merchant_order_id?: string;
}

// ─── Pagos Únicos ─────────────────────────────────────────────────────────────

export type EstadoPagoUnico = 'pending' | 'approved' | 'rejected' | 'cancelled';

export interface MpPagoUnico {
  mpPagoUnicoId: number;
  clienteId: number;
  mpPlanId: number;
  mpSuscripcionId: number | null;
  mpPreferenceId: string;
  mpPaymentId: string | null;
  externalReference: string;
  initPoint: string;
  monto: number;
  moneda: string;
  estado: EstadoPagoUnico;
  fechaCreacion: string;
  fechaPago: string | null;
  fechaReanudacion: string | null;
}

export interface CrearPagoUnicoRequest {
  clienteId?: number;
  mpPlanId: number;
  payerEmail?: string;
}

export interface CrearPagoUnicoResponse {
  mpPagoUnicoId: number;
  initPoint: string;
  estado: string;
  suscripcionPausada: boolean;
}

// ─── Filtros ──────────────────────────────────────────────────────────────────

export interface FiltroSuscripciones {
  estado?: EstadoSuscripcion;
  clienteId?: number;
  pagina: number;
  tamanioPagina: number;
}
