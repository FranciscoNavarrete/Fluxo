import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/services/auth.service';
import { SuscripcionService } from '../../../core/services/suscripcion.service';
import { MpPagoUnico, MpTransaccion } from '../../../shared/models';
import { MonedaArgPipe } from '../../../shared/pipes/moneda-arg.pipe';

interface ItemHistorial {
  tipo: 'recurrente' | 'unico';
  fecha: string;
  estado: string;
  estadoDetalle?: string;
  monto: number;
  moneda: string;
  intento?: number;
}

@Component({
  selector: 'app-historial-pagos',
  standalone: true,
  imports: [CommonModule, MatIconModule, MonedaArgPipe],
  template: `
    <div class="max-w-3xl">
      <h1 class="text-2xl font-semibold text-gray-800 dark:text-slate-100 mb-6">Historial de Pagos</h1>

      <!-- Cargando -->
      <div *ngIf="cargando()" class="flex justify-center py-16">
        <div class="w-8 h-8 border-4 border-indigo-500 border-t-transparent rounded-full animate-spin"></div>
      </div>

      <!-- Sin suscripción -->
      <div *ngIf="!cargando() && !tieneSuscripcion()"
           class="bg-white dark:bg-[#0f1e3d] rounded-xl border border-gray-200 dark:border-indigo-900/30 shadow-sm p-12 text-center">
        <mat-icon class="text-gray-300 dark:text-slate-600 mb-3" style="font-size:48px;width:48px;height:48px">receipt_long</mat-icon>
        <p class="text-gray-500 dark:text-slate-400 text-sm">No tenés ninguna suscripción activa aún.</p>
      </div>

      <!-- Sin pagos -->
      <div *ngIf="!cargando() && tieneSuscripcion() && items().length === 0"
           class="bg-white dark:bg-[#0f1e3d] rounded-xl border border-gray-200 dark:border-indigo-900/30 shadow-sm p-12 text-center">
        <mat-icon class="text-gray-300 dark:text-slate-600 mb-3" style="font-size:48px;width:48px;height:48px">payments</mat-icon>
        <p class="text-gray-500 dark:text-slate-400 text-sm">Todavía no hay pagos registrados para tu suscripción.</p>
        <p class="text-gray-400 dark:text-slate-500 text-xs mt-1">Los cobros aparecerán aquí una vez procesados por Mercado Pago.</p>
      </div>

      <!-- Lista -->
      <div *ngIf="!cargando() && items().length > 0"
           class="bg-white dark:bg-[#0f1e3d] rounded-xl border border-gray-200 dark:border-indigo-900/30 shadow-sm overflow-hidden">
        <div class="divide-y divide-gray-100 dark:divide-indigo-900/20">
          <div *ngFor="let item of items()"
               class="flex items-center justify-between px-5 py-4 hover:bg-gray-50 dark:hover:bg-indigo-900/20 transition-colors">

            <!-- Icono + info -->
            <div class="flex items-center gap-4">
              <div class="w-10 h-10 rounded-full flex items-center justify-center flex-shrink-0"
                   [ngClass]="iconoBg(item.estado)">
                <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px"
                          [ngClass]="iconoColor(item.estado)">
                  {{ iconoNombre(item.estado) }}
                </mat-icon>
              </div>
              <div>
                <div class="flex items-center gap-2">
                  <p class="text-sm font-medium text-gray-800 dark:text-slate-200">{{ etiqueta(item) }}</p>
                  <span *ngIf="item.tipo === 'unico'"
                        class="text-[10px] font-medium px-1.5 py-0.5 rounded-full bg-indigo-100 text-indigo-600">
                    Pago único
                  </span>
                </div>
                <p class="text-xs text-gray-400 dark:text-slate-500">
                  {{ item.fecha | date:'dd/MM/yyyy HH:mm' }}
                  <span *ngIf="item.estadoDetalle" class="ml-2 text-gray-300">· {{ item.estadoDetalle }}</span>
                  <span *ngIf="item.intento" class="ml-2 text-gray-300">· Intento #{{ item.intento }}</span>
                </p>
              </div>
            </div>

            <!-- Monto -->
            <div class="text-right">
              <p class="text-sm font-semibold"
                 [ngClass]="item.estado === 'approved' ? 'text-gray-800 dark:text-slate-200' : 'text-gray-400 dark:text-slate-500'">
                {{ item.monto | monedaArg:item.moneda }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class HistorialPagosComponent implements OnInit {
  private readonly auth    = inject(AuthService);
  private readonly suscSvc = inject(SuscripcionService);

  readonly cargando          = signal(true);
  readonly tieneSuscripcion  = signal(false);
  private readonly transacciones = signal<MpTransaccion[]>([]);
  private readonly pagosUnicos   = signal<MpPagoUnico[]>([]);

  readonly items = computed<ItemHistorial[]>(() => {
    const transItems: ItemHistorial[] = this.transacciones().map(t => ({
      tipo:         'recurrente',
      fecha:        t.fechaProcesado,
      estado:       t.estado,
      estadoDetalle: t.estadoDetalle,
      monto:        t.monto,
      moneda:       t.moneda,
      intento:      t.numeroIntento,
    }));

    const unicoItems: ItemHistorial[] = this.pagosUnicos()
      .filter(p => p.estado === 'approved')
      .map(p => ({
        tipo:   'unico',
        fecha:  p.fechaPago ?? p.fechaCreacion,
        estado: p.estado,
        monto:  p.monto,
        moneda: p.moneda,
      }));

    return [...transItems, ...unicoItems]
      .sort((a, b) => new Date(b.fecha).getTime() - new Date(a.fecha).getTime());
  });

  ngOnInit(): void {
    const clienteId = this.auth.getClienteId();
    if (!clienteId) { this.cargando.set(false); return; }

    this.suscSvc.getMiSuscripcion(clienteId).subscribe({
      next: susc => {
        if (!susc) { this.tieneSuscripcion.set(false); this.cargando.set(false); return; }
        this.tieneSuscripcion.set(true);

        let pendientes = 2;
        const done = () => { if (--pendientes === 0) this.cargando.set(false); };

        this.suscSvc.getTransaccionesSuscripcion(susc.mpSuscripcionId).subscribe({
          next: ts  => { this.transacciones.set(ts); done(); },
          error: () => done(),
        });

        this.suscSvc.getMisPagosUnicos(clienteId).subscribe({
          next: ps  => { this.pagosUnicos.set(ps); done(); },
          error: () => done(),
        });
      },
      error: () => this.cargando.set(false),
    });
  }

  etiqueta(item: ItemHistorial): string {
    if (item.tipo === 'unico') return 'Pago único aprobado';
    if (item.estado === 'approved') return 'Débito aprobado';
    if (item.estado === 'rejected') return 'Débito rechazado';
    if (item.estado === 'pending')  return 'Pago pendiente';
    return item.estado;
  }

  iconoBg(estado: string): string {
    if (estado === 'approved') return 'bg-green-100';
    if (estado === 'rejected') return 'bg-red-100';
    return 'bg-yellow-100';
  }

  iconoColor(estado: string): string {
    if (estado === 'approved') return 'text-green-600';
    if (estado === 'rejected') return 'text-red-500';
    return 'text-yellow-500';
  }

  iconoNombre(estado: string): string {
    if (estado === 'approved') return 'check_circle';
    if (estado === 'rejected') return 'cancel';
    return 'hourglass_empty';
  }
}
