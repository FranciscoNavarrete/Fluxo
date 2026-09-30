import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { SuscripcionService } from '../../../../core/services/suscripcion.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { BadgeEstadoComponent } from '../../../../shared/components/badge-estado/badge-estado.component';
import { MonedaArgPipe } from '../../../../shared/pipes/moneda-arg.pipe';
import {
  ModalConfirmacionComponent,
  ModalConfirmacionData,
  ModalConfirmacionResult,
} from '../../../../shared/components/modal-confirmacion/modal-confirmacion.component';
import { DunningLog, MpPagoUnico, MpTransaccion, MpSuscripcion } from '../../../../shared/models';

@Component({
  selector: 'app-suscripcion-detalle',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatTableModule,
    LoadingSpinnerComponent,
    BadgeEstadoComponent,
    MonedaArgPipe,
  ],
  template: `
    <div class="max-w-5xl mx-auto">

      <!-- Header -->
      <div class="flex items-center gap-3 mb-6">
        <a mat-icon-button routerLink="/admin/suscripciones">
          <mat-icon>arrow_back</mat-icon>
        </a>
        <div class="flex-1">
          <h1 class="text-2xl font-semibold text-gray-800 dark:text-slate-100 leading-tight">Detalle de Suscripción</h1>
          <p class="text-sm text-gray-400 mt-0.5" *ngIf="suscripcion()">
            Gateway ID: {{ suscripcion()!.gatewaySuscripcionId }}
          </p>
        </div>

        <!-- Acciones -->
        <ng-container *ngIf="suscripcion() && suscripcion()!.estado !== 'cancelled'">
          <button
            mat-stroked-button
            color="warn"
            [disabled]="cancelando()"
            (click)="confirmarCancelar()"
          >
            <mat-icon>cancel</mat-icon>
            {{ cancelando() ? 'Cancelando...' : 'Cancelar suscripción' }}
          </button>
        </ng-container>
      </div>

      <app-loading-spinner [visible]="cargando()" mensaje="Cargando detalle..." />

      <ng-container *ngIf="!cargando() && suscripcion()">

        <!-- Banner: cubierto por pago único este mes -->
        <div *ngIf="pagoUnicoEsteMes() && suscripcion()!.estado !== 'authorized'"
             class="mb-4 rounded-xl bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800/40 px-5 py-4 flex gap-3 items-start">
          <mat-icon class="text-green-500 mt-0.5" style="font-size:20px;width:20px;height:20px;line-height:20px">check_circle</mat-icon>
          <div>
            <p class="text-sm font-semibold text-green-800 dark:text-green-300">Cubierto por pago único este mes</p>
            <p class="text-sm text-green-700 dark:text-green-400 mt-0.5">
              El cliente abonó un pago único el
              {{ pagoUnicoEsteMes()!.fechaPago ?? pagoUnicoEsteMes()!.fechaCreacion | date:'dd/MM/yyyy' }}
              · {{ pagoUnicoEsteMes()!.monto | monedaArg:pagoUnicoEsteMes()!.moneda }}
            </p>
          </div>
        </div>

        <!-- Datos principales -->
        <div class="bg-white dark:bg-[#0b1628] rounded-xl border border-gray-200 dark:border-indigo-900/40 shadow-sm p-6 mb-6">
          <div class="flex items-start justify-between flex-wrap gap-4">
            <div class="flex-1">
              <div class="flex items-center gap-3 mb-4">
                <span class="text-lg font-semibold text-gray-800 dark:text-slate-100">{{ suscripcion()!.planNombre }}</span>
                <app-badge-estado [estado]="suscripcion()!.estado" />
              </div>

              <dl class="grid grid-cols-2 sm:grid-cols-3 gap-x-10 gap-y-4 text-sm">
                <div>
                  <dt class="text-xs text-gray-400 mb-0.5">Cliente ID</dt>
                  <dd class="font-medium text-gray-700 dark:text-slate-300">#{{ suscripcion()!.clienteId }}</dd>
                </div>
                <div *ngIf="suscripcion()!.clienteNombre">
                  <dt class="text-xs text-gray-400 mb-0.5">Nombre</dt>
                  <dd class="font-medium text-gray-700 dark:text-slate-300">{{ suscripcion()!.clienteNombre }}</dd>
                </div>
                <div>
                  <dt class="text-xs text-gray-400 mb-0.5">Email</dt>
                  <dd class="font-medium text-gray-700 text-xs">{{ suscripcion()!.clienteEmail }}</dd>
                </div>
                <div>
                  <dt class="text-xs text-gray-400 mb-0.5">Inicio</dt>
                  <dd class="font-medium text-gray-700 dark:text-slate-300">{{ suscripcion()!.fechaInicio | date:'dd/MM/yyyy' }}</dd>
                </div>
                <div>
                  <dt class="text-xs text-gray-400 mb-0.5">Próximo cobro</dt>
                  <dd class="font-medium text-gray-700 dark:text-slate-300">
                    {{ suscripcion()!.proximoCobro ? (suscripcion()!.proximoCobro! | date:'dd/MM/yyyy') : '—' }}
                  </dd>
                </div>
                <div>
                  <dt class="text-xs text-gray-400 mb-0.5">Último cobro</dt>
                  <dd class="font-medium text-gray-700 dark:text-slate-300">
                    {{ suscripcion()!.ultimoCobro ? (suscripcion()!.ultimoCobro! | date:'dd/MM/yyyy') : '—' }}
                  </dd>
                </div>
                <div>
                  <dt class="text-xs text-gray-400 mb-0.5">Reintentos</dt>
                  <dd class="font-medium" [class.text-red-600]="suscripcion()!.intentosReintento > 2">
                    {{ suscripcion()!.intentosReintento }} / {{ suscripcion()!.maxReintentos }}
                  </dd>
                </div>
                <div *ngIf="suscripcion()!.mpPayerId">
                  <dt class="text-xs text-gray-400 mb-0.5">MP Payer ID</dt>
                  <dd class="font-medium text-gray-700 font-mono text-xs">{{ suscripcion()!.mpPayerId }}</dd>
                </div>
                <div *ngIf="suscripcion()!.motivoCancelacion">
                  <dt class="text-xs text-gray-400 mb-0.5">Motivo cancelación</dt>
                  <dd class="font-medium text-red-600">{{ suscripcion()!.motivoCancelacion }}</dd>
                </div>
              </dl>
            </div>
          </div>
        </div>

        <!-- Tabs -->
        <mat-tab-group animationDuration="200ms">

          <mat-tab>
            <ng-template mat-tab-label>
              <mat-icon style="font-size:16px;width:16px;height:16px;line-height:16px;margin-right:6px">receipt_long</mat-icon>
              Pagos ({{ todosPagos().length }})
            </ng-template>

            <div class="mt-4 bg-white dark:bg-[#0b1628] rounded-xl border border-gray-200 dark:border-indigo-900/40 overflow-hidden">
              <table mat-table [dataSource]="todosPagos()" class="w-full">
                <ng-container matColumnDef="tipo">
                  <th mat-header-cell *matHeaderCellDef>Tipo</th>
                  <td mat-cell *matCellDef="let row">
                    <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                          [ngClass]="row.tipo === 'Pago único'
                            ? 'bg-indigo-100 text-indigo-700'
                            : 'bg-gray-100 text-gray-600'">
                      {{ row.tipo }}
                    </span>
                  </td>
                </ng-container>
                <ng-container matColumnDef="fecha">
                  <th mat-header-cell *matHeaderCellDef>Fecha</th>
                  <td mat-cell *matCellDef="let row">{{ row.fecha | date:'dd/MM/yyyy HH:mm' }}</td>
                </ng-container>
                <ng-container matColumnDef="monto">
                  <th mat-header-cell *matHeaderCellDef>Monto</th>
                  <td mat-cell *matCellDef="let row" class="font-medium">{{ row.monto | monedaArg:row.moneda }}</td>
                </ng-container>
                <ng-container matColumnDef="estado">
                  <th mat-header-cell *matHeaderCellDef>Estado</th>
                  <td mat-cell *matCellDef="let row"><app-badge-estado [estado]="row.estado" /></td>
                </ng-container>
                <ng-container matColumnDef="intento">
                  <th mat-header-cell *matHeaderCellDef>Intento N°</th>
                  <td mat-cell *matCellDef="let row" class="text-center">{{ row.intento ?? '—' }}</td>
                </ng-container>
                <tr mat-header-row *matHeaderRowDef="columnasPagos" class="bg-gray-50 dark:bg-indigo-950/40"></tr>
                <tr mat-row *matRowDef="let row; columns: columnasPagos"
                    class="border-b border-gray-100 dark:border-indigo-900/20 hover:bg-blue-50 dark:hover:bg-indigo-900/20 transition-colors"></tr>
              </table>
              <div *ngIf="!todosPagos().length" class="py-10 text-center text-gray-400 text-sm">Sin pagos registrados</div>
            </div>
          </mat-tab>

          <mat-tab>
            <ng-template mat-tab-label>
              <mat-icon style="font-size:16px;width:16px;height:16px;line-height:16px;margin-right:6px">sync_problem</mat-icon>
              Dunning ({{ dunningLogs().length }})
            </ng-template>

            <div class="mt-4 bg-white dark:bg-[#0b1628] rounded-xl border border-gray-200 dark:border-indigo-900/40 overflow-hidden">
              <table mat-table [dataSource]="dunningLogs()" class="w-full">
                <ng-container matColumnDef="fecha">
                  <th mat-header-cell *matHeaderCellDef>Fecha</th>
                  <td mat-cell *matCellDef="let row">{{ row.fechaEjecucion | date:'dd/MM/yyyy HH:mm' }}</td>
                </ng-container>
                <ng-container matColumnDef="accion">
                  <th mat-header-cell *matHeaderCellDef>Acción</th>
                  <td mat-cell *matCellDef="let row" class="font-medium">{{ row.accion }}</td>
                </ng-container>
                <ng-container matColumnDef="canal">
                  <th mat-header-cell *matHeaderCellDef>Canal</th>
                  <td mat-cell *matCellDef="let row">
                    <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-indigo-100 text-indigo-700">
                      {{ row.canal }}
                    </span>
                  </td>
                </ng-container>
                <ng-container matColumnDef="diasVencido">
                  <th mat-header-cell *matHeaderCellDef>Días vencido</th>
                  <td mat-cell *matCellDef="let row" class="text-center">{{ row.diasVencido }}</td>
                </ng-container>
                <tr mat-header-row *matHeaderRowDef="columnasDunning" class="bg-gray-50 dark:bg-indigo-950/40"></tr>
                <tr mat-row *matRowDef="let row; columns: columnasDunning" class="border-b border-gray-100 dark:border-indigo-900/20"></tr>
              </table>
              <div *ngIf="!dunningLogs().length" class="py-10 text-center text-gray-400 text-sm">Sin logs de dunning</div>
            </div>
          </mat-tab>

        </mat-tab-group>
      </ng-container>
    </div>
  `,
  styles: [`
    th.mat-header-cell { font-weight: 600; color: #374151; font-size: 0.75rem;
      text-transform: uppercase; letter-spacing: 0.05em; padding: 10px 16px; }
    td.mat-cell { font-size: 0.875rem; padding: 10px 16px; color: #1f2937; }
    :host-context(.dark) th.mat-header-cell { color: #7c8ba6; }
    :host-context(.dark) td.mat-cell { color: #cbd5e1; }
  `],
})
export class SuscripcionDetalleComponent implements OnInit {
  private readonly route    = inject(ActivatedRoute);
  private readonly suscSvc  = inject(SuscripcionService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog   = inject(MatDialog);

  readonly cargando    = signal(false);
  readonly cancelando  = signal(false);
  readonly suscripcion = signal<MpSuscripcion | null>(null);
  readonly pagos       = signal<MpTransaccion[]>([]);
  readonly pagosUnicos = signal<MpPagoUnico[]>([]);
  readonly dunningLogs = signal<DunningLog[]>([]);

  readonly pagoUnicoEsteMes = computed(() => {
    const ahora = new Date();
    return this.pagosUnicos().find(p => {
      if (p.estado !== 'approved') return false;
      const fecha = new Date(p.fechaPago ?? p.fechaCreacion);
      return fecha.getFullYear() === ahora.getFullYear() && fecha.getMonth() === ahora.getMonth();
    }) ?? null;
  });

  readonly todosPagos = computed(() => {
    const recurrentes = this.pagos().map(t => ({
      tipo: 'Débito',
      fecha: t.fechaProcesado,
      monto: t.monto,
      moneda: t.moneda,
      estado: t.estado,
      intento: t.numeroIntento,
    }));
    const unicos = this.pagosUnicos()
      .filter(p => p.estado === 'approved')
      .map(p => ({
        tipo: 'Pago único',
        fecha: p.fechaPago ?? p.fechaCreacion,
        monto: p.monto,
        moneda: p.moneda,
        estado: p.estado,
        intento: null,
      }));
    return [...recurrentes, ...unicos]
      .sort((a, b) => new Date(b.fecha).getTime() - new Date(a.fecha).getTime());
  });

  readonly columnasPagos   = ['tipo', 'fecha', 'monto', 'estado', 'intento'];
  readonly columnasDunning = ['fecha', 'accion', 'canal', 'diasVencido'];

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) return;
    this.cargar(id);
  }

  confirmarCancelar(): void {
    const s = this.suscripcion();
    if (!s) return;

    const data: ModalConfirmacionData = {
      titulo:         'Cancelar suscripción',
      mensaje:        `¿Confirmás que querés cancelar la suscripción de ${s.clienteEmail}? Esta acción cancela el cobro en Mercado Pago y no puede deshacerse.`,
      labelConfirmar: 'Sí, cancelar',
      tipo:           'danger',
    };

    this.dialog
      .open<ModalConfirmacionComponent, ModalConfirmacionData, ModalConfirmacionResult>(
        ModalConfirmacionComponent, { data, width: '460px' },
      )
      .afterClosed()
      .subscribe(res => {
        if (res?.confirmado) this.cancelar(s.mpSuscripcionId);
      });
  }

  private cancelar(id: number): void {
    this.cancelando.set(true);
    this.suscSvc.cancelarSuscripcion({ mpSuscripcionId: id, motivo: 'Cancelado por administrador' }).subscribe({
      next: () => {
        this.snackBar.open('Suscripción cancelada correctamente', 'OK', { duration: 4000 });
        this.cancelando.set(false);
        this.cargar(id);
      },
      error: err => {
        this.snackBar.open(err.message, 'OK', { duration: 5000 });
        this.cancelando.set(false);
      },
    });
  }

  private cargar(id: number): void {
    this.cargando.set(true);
    forkJoin({
      suscripcion: this.suscSvc.getSuscripcionDetalle(id),
      pagos:       this.suscSvc.getTransaccionesSuscripcion(id),
      dunning:     this.suscSvc.getDunningLogs(id),
    }).subscribe({
      next: ({ suscripcion, pagos, dunning }) => {
        this.suscripcion.set(suscripcion);
        this.pagos.set(pagos);
        this.dunningLogs.set(dunning);
        // Cargar pagos únicos del cliente en paralelo (no bloquea la pantalla si falla)
        this.suscSvc.getMisPagosUnicos(suscripcion.clienteId).subscribe({
          next: pu => this.pagosUnicos.set(pu),
          error: () => {},
        });
        this.cargando.set(false);
      },
      error: err => {
        this.snackBar.open(err.message, 'OK', { duration: 5000 });
        this.cargando.set(false);
      },
    });
  }
}
