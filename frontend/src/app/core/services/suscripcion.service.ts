import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiService } from './api.service';
import {
  BackendPaginado,
  CancelarSuscripcionRequest,
  CrearPagoUnicoRequest,
  CrearPagoUnicoResponse,
  CrearPlanRequest,
  CrearSuscripcionRequest,
  CrearSuscripcionResponse,
  DunningLog,
  DunningResponse,
  EditarPlanRequest,
  FiltroSuscripciones,
  MpPagoUnico,
  MpPlan,
  MpSuscripcion,
  MpTransaccion,
  PaginationResult,
} from '../../shared/models';

@Injectable({ providedIn: 'root' })
export class SuscripcionService {
  private readonly api = inject(ApiService);

  // ─── Planes ──────────────────────────────────────────────────────────────

  getPlanes(): Observable<MpPlan[]> {
    return this.api.get<MpPlan[]>('mp/planes');
  }

  getPlanesActivos(): Observable<MpPlan[]> {
    return this.api.get<MpPlan[]>('mp/planes');
  }

  crearPlan(req: CrearPlanRequest): Observable<MpPlan> {
    return this.api.post<MpPlan>('mp/planes', req);
  }

  editarPlan(req: EditarPlanRequest): Observable<MpPlan> {
    return this.api.put<MpPlan>('mp/planes', req);
  }

  eliminarPlan(mpPlanId: number): Observable<boolean> {
    return this.api.delete<boolean>(`mp/planes/${mpPlanId}`);
  }

  // Toggle usando el endpoint de edición (Activo = true/false)
  togglePlan(plan: MpPlan, activo: boolean): Observable<MpPlan> {
    const req: EditarPlanRequest = {
      mpPlanId:      plan.mpPlanId,
      nombre:        plan.nombre,
      descripcion:   plan.descripcion,
      monto:         plan.monto,
      moneda:        plan.moneda,
      tipoFrecuencia: plan.tipoFrecuencia,
      frecuencia:    plan.frecuencia,
      diasGratis:    plan.diasGratis,
      repeticiones:  plan.repeticiones,
      montoPrimerCobro: plan.montoPrimerCobro,
      montoPromo:    plan.montoPromo,
      mesesPromo:    plan.mesesPromo,
      activo,
    };
    return this.api.put<MpPlan>('mp/planes', req);
  }

  // ─── Suscripciones (cliente) ──────────────────────────────────────────────

  getMiSuscripcion(clienteId: number): Observable<MpSuscripcion | null> {
    return this.api
      .get<BackendPaginado<MpSuscripcion>>('mp/suscripciones', {
        clienteId,
        pagina:        1,
        tamanioPagina: 1,
      })
      .pipe(
        map(res => res.listaResultado?.[0] ?? null),
        catchError(() => of(null)),
      );
  }

  getMisSuscripciones(clienteId: number): Observable<MpSuscripcion[]> {
    return this.api
      .get<BackendPaginado<MpSuscripcion>>('mp/suscripciones', {
        clienteId,
        pagina:        1,
        tamanioPagina: 20,
      })
      .pipe(
        map(res => res.listaResultado ?? []),
        catchError(() => of([])),
      );
  }

  crearConRedirect(req: CrearSuscripcionRequest): Observable<CrearSuscripcionResponse> {
    return this.api.post<CrearSuscripcionResponse>(
      'mp/suscripciones/crear-con-redirect',
      req,
    );
  }

  cancelarSuscripcion(req: CancelarSuscripcionRequest): Observable<boolean> {
    return this.api.post<boolean>('mp/suscripciones/cancelar', req);
  }

  // ─── Suscripciones (admin) ────────────────────────────────────────────────

  getSuscripciones(filtros: FiltroSuscripciones): Observable<PaginationResult<MpSuscripcion>> {
    const params: Record<string, string | number | boolean> = {
      pagina:        filtros.pagina,
      tamanioPagina: filtros.tamanioPagina,
    };
    if (filtros.estado)    params['estado']    = filtros.estado;
    if (filtros.clienteId) params['clienteId'] = filtros.clienteId;

    return this.api
      .get<BackendPaginado<MpSuscripcion>>('mp/suscripciones', params)
      .pipe(
        map(res => ({
          items:      res.listaResultado,
          totalItems: res.totalFilas,
          pageNumber: filtros.pagina,
          pageSize:   filtros.tamanioPagina,
          totalPages: Math.ceil(res.totalFilas / filtros.tamanioPagina),
        })),
      );
  }

  getSuscripcionDetalle(mpSuscripcionId: number): Observable<MpSuscripcion> {
    return this.api.get<MpSuscripcion>(`mp/suscripciones/${mpSuscripcionId}`);
  }

  // ─── Transacciones ────────────────────────────────────────────────────────
  getTransaccionesSuscripcion(mpSuscripcionId: number): Observable<MpTransaccion[]> {
    return this.api.get<MpTransaccion[]>(`mp/suscripciones/${mpSuscripcionId}/transacciones`);
  }

  // ─── Pagos Únicos ─────────────────────────────────────────────────────────

  crearPagoUnico(req: CrearPagoUnicoRequest): Observable<CrearPagoUnicoResponse> {
    return this.api.post<CrearPagoUnicoResponse>('mp/pagos-unicos/crear', req);
  }

  getMisPagosUnicos(clienteId?: number): Observable<MpPagoUnico[]> {
    const params: Record<string, number> = {};
    if (clienteId) params['clienteId'] = clienteId;
    return this.api.get<MpPagoUnico[]>('mp/pagos-unicos/mis-pagos', params);
  }

  eliminarPagoUnico(mpPagoUnicoId: number): Observable<boolean> {
    return this.api.delete<boolean>(`mp/pagos-unicos/${mpPagoUnicoId}`);
  }

  // ─── Dunning ──────────────────────────────────────────────────────────────

  ejecutarDunning(): Observable<DunningResponse> {
    return this.api.post<DunningResponse>('admin/dunning/ejecutar', {});
  }

  // El backend no expone logs de dunning por endpoint aún.
  getDunningLogs(_mpSuscripcionId: number): Observable<DunningLog[]> {
    return of([]);
  }
}
