import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';
import { SuscripcionService } from '../../../core/services/suscripcion.service';
import { MpService } from '../../../core/services/mp.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { BadgeEstadoComponent } from '../../../shared/components/badge-estado/badge-estado.component';
import { MonedaArgPipe } from '../../../shared/pipes/moneda-arg.pipe';
import { MpPlan, MpPagoUnico, MpSuscripcion } from '../../../shared/models';

@Component({
  selector: 'app-pago-unico',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    LoadingSpinnerComponent,
    BadgeEstadoComponent,
    MonedaArgPipe,
  ],
  template: `
    <div class="max-w-4xl mx-auto">
      <h1 class="text-2xl font-semibold text-gray-800 dark:text-slate-100 mb-1">Pago Único</h1>
      <p class="text-gray-500 dark:text-slate-400 text-sm mb-6">
        Pagá el monto de un plan sin suscribirte.
        Si tenés una suscripción activa, se pausará este mes y se retomará el siguiente.
      </p>

      <app-loading-spinner [visible]="cargando()" mensaje="Cargando información..." />

      <ng-container *ngIf="!cargando()">

        <!-- Aviso si tiene suscripción activa -->
        <div *ngIf="suscripcion() && suscripcion()!.estado === 'authorized'"
             class="mb-5 rounded-xl border border-blue-200 dark:border-blue-900/40 bg-blue-50 dark:bg-blue-900/20 px-5 py-4 flex gap-3 items-start">
          <mat-icon class="text-blue-500 mt-0.5 shrink-0">info</mat-icon>
          <div class="text-sm">
            <p class="font-medium text-blue-800 dark:text-blue-300">Tenés una suscripción activa — Plan {{ suscripcion()!.planNombre }}</p>
            <p class="text-blue-600 dark:text-blue-400 mt-0.5">
              Si realizás un pago único, tu suscripción se pausará automáticamente este mes
              y se reanudará en el próximo ciclo.
            </p>
          </div>
        </div>

        <!-- Listado de planes -->
        <div *ngIf="planes().length" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5 mb-8">
          <mat-card
            *ngFor="let plan of planes()"
            class="cursor-pointer hover:shadow-md transition-shadow border-2"
            [class.border-primary-600]="planSeleccionado()?.mpPlanId === plan.mpPlanId"
            [class.border-transparent]="planSeleccionado()?.mpPlanId !== plan.mpPlanId"
            (click)="seleccionarPlan(plan)"
          >
            <mat-card-header>
              <mat-card-title class="text-base font-semibold">{{ plan.nombre }}</mat-card-title>
              <mat-card-subtitle>{{ plan.descripcion }}</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content class="pt-4">
              <p class="text-3xl font-bold text-primary-700">
                {{ plan.monto | monedaArg:plan.moneda }}
              </p>
              <p class="text-sm text-gray-500 dark:text-slate-500 mt-1">pago único</p>
            </mat-card-content>
            <mat-card-actions class="px-4 pb-4">
              <button
                mat-flat-button
                color="primary"
                class="w-full"
                [disabled]="procesando()"
                (click)="$event.stopPropagation(); pagar(plan)"
              >
                <mat-icon>payment</mat-icon>
                Pagar ahora
              </button>
            </mat-card-actions>
          </mat-card>
        </div>

        <div *ngIf="errorPlanes()" class="text-center text-red-500 mt-12 text-sm">
          Error al cargar planes: {{ errorPlanes() }}
        </div>
        <p *ngIf="!planes().length && !errorPlanes()" class="text-center text-gray-400 mt-12">
          No hay planes disponibles en este momento.
        </p>

        <!-- Historial de pagos únicos -->
        <ng-container *ngIf="historial().length">
          <h2 class="text-lg font-semibold text-gray-700 dark:text-slate-300 mb-3">Historial de pagos únicos</h2>
          <div class="bg-white dark:bg-[#0f1e3d] rounded-xl border border-gray-200 dark:border-indigo-900/30 shadow-sm overflow-hidden">
            <table class="w-full text-sm">
              <thead class="bg-gray-50 dark:bg-[#080f1e]/60 border-b border-gray-200 dark:border-indigo-900/20">
                <tr>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-slate-500 uppercase tracking-wide">Fecha</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-slate-500 uppercase tracking-wide">Monto</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-slate-500 uppercase tracking-wide">Estado</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-slate-500 uppercase tracking-wide">Acción</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let p of historial(); let last = last"
                    [class.border-b]="!last"
                    class="border-gray-100 dark:border-indigo-900/20 hover:bg-gray-50 dark:hover:bg-indigo-900/10 transition-colors">
                  <td class="px-4 py-3 text-gray-700 dark:text-slate-400">{{ p.fechaCreacion | date:'dd/MM/yyyy' }}</td>
                  <td class="px-4 py-3 font-medium text-gray-800 dark:text-slate-200">{{ p.monto | monedaArg:p.moneda }}</td>
                  <td class="px-4 py-3">
                    <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium"
                          [ngClass]="{
                            'bg-green-100 text-green-700':  p.estado === 'approved',
                            'bg-red-100 text-red-700':      p.estado === 'rejected' || p.estado === 'cancelled',
                            'bg-yellow-100 text-yellow-700': p.estado === 'pending'
                          }">
                      <mat-icon class="text-[14px] w-[14px] h-[14px] leading-none">
                        {{ p.estado === 'approved' ? 'check_circle' : p.estado === 'pending' ? 'schedule' : 'cancel' }}
                      </mat-icon>
                      {{ p.estado === 'approved' ? 'Aprobado' : p.estado === 'pending' ? 'Pendiente' : 'Rechazado' }}
                    </span>
                  </td>
                  <td class="px-4 py-3">
                    <div *ngIf="p.estado === 'pending'" class="flex items-center gap-3">
                      <a [href]="p.initPoint" target="_blank"
                         class="text-xs text-primary-600 hover:underline font-medium">
                        Completar pago
                      </a>
                      <button (click)="eliminar(p.mpPagoUnicoId)"
                              class="text-red-400 hover:text-red-600 transition-colors"
                              title="Eliminar">
                        <mat-icon style="font-size:16px;width:16px;height:16px;line-height:16px">delete</mat-icon>
                      </button>
                    </div>
                    <span *ngIf="p.estado !== 'pending'" class="text-gray-400 dark:text-slate-600 text-xs">—</span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </ng-container>

      </ng-container>
    </div>
  `,
})
export class PagoUnicoComponent implements OnInit {
  private readonly auth     = inject(AuthService);
  private readonly suscSvc  = inject(SuscripcionService);
  private readonly mpSvc    = inject(MpService);
  private readonly snackBar = inject(MatSnackBar);

  readonly cargando         = signal(false);
  readonly procesando       = signal(false);
  readonly planes           = signal<MpPlan[]>([]);
  readonly suscripcion      = signal<MpSuscripcion | null>(null);
  readonly planSeleccionado = signal<MpPlan | null>(null);
  readonly historial        = signal<MpPagoUnico[]>([]);
  readonly errorPlanes      = signal('');

  ngOnInit(): void {
    this.cargarDatos();
  }

  private cargarDatos(): void {
    this.cargando.set(true);
    this.cargarPlanes();

    const clienteId = this.auth.getClienteId();
    if (clienteId) {
      this.suscSvc.getMiSuscripcion(clienteId).subscribe({
        next: susc => this.suscripcion.set(susc),
        error: () => {},
      });
      this.cargarHistorial();
    }
  }

  private cargarPlanes(): void {
    this.suscSvc.getPlanesActivos().subscribe({
      next: planes => {
        this.planes.set(planes.filter(p => p.activo));
        this.cargando.set(false);
      },
      error: (err) => {
        this.errorPlanes.set(err?.message ?? 'Error desconocido al cargar planes');
        this.cargando.set(false);
      },
    });
  }

  private cargarHistorial(): void {
    this.suscSvc.getMisPagosUnicos().subscribe({
      next: h => this.historial.set(h),
      error: () => {},
    });
  }

  seleccionarPlan(plan: MpPlan): void {
    this.planSeleccionado.set(plan);
  }

  eliminar(mpPagoUnicoId: number): void {
    this.suscSvc.eliminarPagoUnico(mpPagoUnicoId).subscribe({
      next: () => this.historial.update(h => h.filter(p => p.mpPagoUnicoId !== mpPagoUnicoId)),
      error: err => this.snackBar.open(err.message ?? 'No se pudo eliminar.', 'OK', {
        duration: 4000, panelClass: ['snack-error'],
      }),
    });
  }

  pagar(plan: MpPlan): void {
    const usuario   = this.auth.usuario();
    const clienteId = this.auth.getClienteId();
    if (!usuario || !clienteId) return;

    this.procesando.set(true);
    this.suscSvc.crearPagoUnico({
      mpPlanId:   plan.mpPlanId,
      payerEmail: usuario.email,
    }).subscribe({
      next: res => this.mpSvc.redirectToInitPoint(res.initPoint),
      error: err => {
        this.snackBar.open(err.message ?? 'Error al generar el pago.', 'OK', {
          duration: 5000,
          panelClass: ['snack-error'],
        });
        this.procesando.set(false);
      },
    });
  }
}
