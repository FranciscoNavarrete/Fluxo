import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';
import { SuscripcionService } from '../../../core/services/suscripcion.service';
import { MpService } from '../../../core/services/mp.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { BadgeEstadoComponent } from '../../../shared/components/badge-estado/badge-estado.component';
import {
  ModalConfirmacionComponent,
  ModalConfirmacionData,
  ModalConfirmacionResult,
} from '../../../shared/components/modal-confirmacion/modal-confirmacion.component';
import { MonedaArgPipe } from '../../../shared/pipes/moneda-arg.pipe';
import { MpPagoUnico, MpPlan, MpSuscripcion } from '../../../shared/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    LoadingSpinnerComponent,
    BadgeEstadoComponent,
    MonedaArgPipe,
  ],
  template: `
    <div class="max-w-4xl mx-auto">
      <h1 class="text-2xl font-semibold text-gray-800 dark:text-slate-100 mb-6">Mi Suscripción</h1>

      <app-loading-spinner [visible]="cargando()" mensaje="Cargando tu información..." />

      <ng-container *ngIf="!cargando()">

        <!-- ── Suscripción activa ── -->
        <ng-container *ngIf="suscripcionActiva()">

          <!-- Banner: suscripción pending (no completada en MP) -->
          <div *ngIf="suscripcionActiva()!.estado === 'pending'"
               class="mb-4 rounded-xl bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800/40 px-5 py-4 flex gap-3 items-start">
            <mat-icon class="text-blue-500 mt-0.5" style="font-size:20px;width:20px;height:20px;line-height:20px">pending_actions</mat-icon>
            <div class="flex-1">
              <p class="text-sm font-semibold text-blue-800 dark:text-blue-300">Suscripción pendiente de activación</p>
              <p class="text-sm text-blue-600 dark:text-blue-400 mt-0.5">
                Todavía no completaste el proceso de pago en Mercado Pago. Tu suscripción quedará activa una vez que lo hagas.
              </p>
            </div>
            <button *ngIf="suscripcionActiva()!.initPoint"
                    mat-flat-button color="primary"
                    (click)="irACompletarSuscripcion()"
                    class="shrink-0 mt-0.5">
              <mat-icon>open_in_new</mat-icon> Completar
            </button>
          </div>

          <!-- Banner: cubierto por pago único -->
          <div *ngIf="pagoUnicoEsteMes() && suscripcionActiva()!.estado !== 'authorized'"
               class="mb-4 rounded-xl bg-green-50 border border-green-200 px-5 py-4 flex gap-3 items-start">
            <mat-icon class="text-green-500 mt-0.5" style="font-size:20px;width:20px;height:20px;line-height:20px">check_circle</mat-icon>
            <div>
              <p class="text-sm font-semibold text-green-800">Estás al día este mes</p>
              <p class="text-sm text-green-700 mt-0.5">
                Tu pago único fue aprobado el
                {{ pagoUnicoEsteMes()!.fechaPago ?? pagoUnicoEsteMes()!.fechaCreacion | date:'dd/MM/yyyy' }}.
                Tu suscripción se reanudará en el próximo ciclo.
              </p>
            </div>
          </div>

          <!-- Banner: paused sin pago único -->
          <div *ngIf="!pagoUnicoEsteMes() && suscripcionActiva()!.estado === 'paused'"
               class="mb-4 rounded-xl bg-orange-50 border border-orange-200 px-5 py-4 flex gap-3 items-start">
            <mat-icon class="text-orange-500 mt-0.5" style="font-size:20px;width:20px;height:20px;line-height:20px">warning_amber</mat-icon>
            <div>
              <p class="text-sm font-semibold text-orange-800">Tu suscripción está pausada</p>
              <p class="text-sm text-orange-600 mt-0.5">No se pudo procesar el débito automático. Podés abonar este mes con un pago único.</p>
            </div>
          </div>

          <!-- Banner: suspendida -->
          <div *ngIf="suscripcionActiva()!.estado === 'suspended'"
               class="mb-4 rounded-xl bg-red-50 border border-red-200 px-5 py-4 flex gap-3 items-start">
            <mat-icon class="text-red-500 mt-0.5" style="font-size:20px;width:20px;height:20px;line-height:20px">error_outline</mat-icon>
            <div>
              <p class="text-sm font-semibold text-red-800">Tu suscripción está suspendida</p>
              <p class="text-sm text-red-600 mt-0.5">Se agotaron los reintentos de cobro. Contactá al soporte para regularizar tu situación.</p>
            </div>
          </div>

          <mat-card class="mb-4">
            <mat-card-content class="p-6">
              <div class="flex items-start justify-between flex-wrap gap-4">
                <div>
                  <div class="flex items-center gap-3 mb-4">
                    <span class="text-lg font-semibold text-gray-800 dark:text-slate-100">Plan {{ suscripcionActiva()!.planNombre }}</span>
                    <app-badge-estado [estado]="suscripcionActiva()!.estado" />
                    <!-- Badge extra: cubierto este mes -->
                    <span *ngIf="pagoUnicoEsteMes() && suscripcionActiva()!.estado !== 'authorized'"
                          class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
                      <mat-icon style="font-size:12px;width:12px;height:12px;line-height:12px">verified</mat-icon>
                      Cubierto este mes
                    </span>
                  </div>
                  <dl class="grid grid-cols-2 gap-x-8 gap-y-3 text-sm">
                    <div>
                      <dt class="text-gray-400 dark:text-slate-500">Inicio</dt>
                      <dd class="font-medium text-gray-700 dark:text-slate-200">{{ suscripcionActiva()!.fechaInicio | date:'dd/MM/yyyy' }}</dd>
                    </div>
                    <div>
                      <dt class="text-gray-400 dark:text-slate-500">Próximo cobro</dt>
                      <dd class="font-medium text-gray-700 dark:text-slate-200">
                        {{ suscripcionActiva()!.proximoCobro ? (suscripcionActiva()!.proximoCobro! | date:'dd/MM/yyyy') : '—' }}
                      </dd>
                    </div>
                    <div>
                      <dt class="text-gray-400 dark:text-slate-500">Último cobro</dt>
                      <dd class="font-medium text-gray-700 dark:text-slate-200">
                        {{ suscripcionActiva()!.ultimoCobro ? (suscripcionActiva()!.ultimoCobro! | date:'dd/MM/yyyy') : '—' }}
                      </dd>
                    </div>
                    <div>
                      <dt class="text-gray-400 dark:text-slate-500">Monto</dt>
                      <dd class="font-medium text-gray-700 dark:text-slate-200">{{ suscripcionActiva()!.planMonto | monedaArg:suscripcionActiva()!.planMoneda }}</dd>
                    </div>
                  </dl>
                </div>
                <div class="flex flex-col gap-3">
                  <!-- Pagar este mes: para authorized (pago adelantado) o paused sin cobertura -->
                  <button
                    *ngIf="suscripcionActiva()!.estado === 'authorized' || (suscripcionActiva()!.estado === 'paused' && !pagoUnicoEsteMes())"
                    mat-flat-button color="primary" [disabled]="procesando()" (click)="pagarEsteMes()">
                    <mat-icon>payment</mat-icon> Pagar este mes
                  </button>
                  <button mat-stroked-button color="warn" [disabled]="procesando()" (click)="abrirModalCancelar()">
                    <mat-icon>cancel</mat-icon> Cancelar suscripción
                  </button>
                </div>
              </div>
            </mat-card-content>
          </mat-card>

        </ng-container>

        <!-- ── Sin suscripción activa: planes disponibles ── -->
        <ng-container *ngIf="!suscripcionActiva()">
          <p class="text-gray-500 dark:text-slate-400 mb-6 text-sm">
            {{ historial().length ? 'No tenés una suscripción activa. Podés suscribirte a un nuevo plan.' : 'Aún no tenés una suscripción activa. Elegí el plan que mejor se adapte a vos.' }}
          </p>

          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
            <mat-card *ngFor="let plan of planes()"
              class="cursor-pointer hover:shadow-md transition-shadow border-2 flex flex-col"
              [class.border-primary-600]="planSeleccionado()?.mpPlanId === plan.mpPlanId"
              [class.border-transparent]="planSeleccionado()?.mpPlanId !== plan.mpPlanId"
              (click)="seleccionarPlan(plan)">
              <mat-card-header>
                <mat-card-title class="text-base font-semibold">{{ plan.nombre }}</mat-card-title>
                <mat-card-subtitle>{{ plan.descripcion }}</mat-card-subtitle>
              </mat-card-header>
              <mat-card-content class="pt-4 flex-1">
                <p class="text-3xl font-bold text-primary-700">{{ plan.monto | monedaArg:plan.moneda }}</p>
                <p class="text-sm text-gray-500 mt-1">/ {{ plan.frecuencia }} {{ plan.tipoFrecuencia === 'months' ? 'mes' : 'día' }}</p>
                <p class="mt-2 text-xs font-medium"
                   [class.text-green-600]="plan.diasGratis > 0"
                   [class.invisible]="plan.diasGratis === 0">
                  {{ plan.diasGratis }} días gratis
                </p>
              </mat-card-content>
              <mat-card-content class="px-4 pb-2">
                <div class="flex items-center gap-2">
                  <label class="text-xs text-gray-500 whitespace-nowrap">Día de cobro:</label>
                  <input type="number" [(ngModel)]="diasCobro[plan.mpPlanId]" placeholder="Ej: 10" min="1" max="31"
                    class="w-20 border border-gray-200 dark:border-indigo-900/40 rounded-lg px-2 py-1 text-sm text-center outline-none
                           bg-white dark:bg-[#080f1e]/60 text-gray-800 dark:text-slate-200"
                    (click)="$event.stopPropagation()" />
                  <span class="text-xs text-gray-400 dark:text-slate-500">(opcional)</span>
                </div>
              </mat-card-content>
              <mat-card-actions class="px-4 pb-4 mt-auto">
                <button mat-flat-button color="primary" class="w-full" [disabled]="procesando()"
                  (click)="$event.stopPropagation(); suscribirse(plan)">
                  <mat-icon>rocket_launch</mat-icon> Suscribirme
                </button>
              </mat-card-actions>
            </mat-card>
          </div>

          <p *ngIf="!planes().length" class="text-center text-gray-400 mt-12">
            No hay planes disponibles en este momento.
          </p>
        </ng-container>

        <!-- ── Historial de suscripciones anteriores ── -->
        <div *ngIf="historial().length" class="mt-8">
          <button class="flex items-center gap-2 text-sm text-gray-500 dark:text-slate-400 hover:text-gray-700 dark:hover:text-slate-200 mb-3 transition-colors"
                  (click)="mostrarHistorial.set(!mostrarHistorial())">
            <mat-icon class="text-[18px] transition-transform"
                      [class.rotate-180]="mostrarHistorial()">expand_more</mat-icon>
            {{ mostrarHistorial() ? 'Ocultar' : 'Ver' }} historial de suscripciones ({{ historial().length }})
          </button>

          <div *ngIf="mostrarHistorial()" class="bg-white dark:bg-[#0f1e3d] rounded-xl border border-gray-200 dark:border-indigo-900/30 shadow-sm overflow-hidden">
            <table class="w-full text-sm">
              <thead class="bg-gray-50 dark:bg-[#080f1e]/60 border-b border-gray-200 dark:border-indigo-900/20">
                <tr>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-slate-500 uppercase tracking-wide">Plan</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-slate-500 uppercase tracking-wide">Estado</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-slate-500 uppercase tracking-wide">Inicio</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-slate-500 uppercase tracking-wide">Monto</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let s of historial(); let last = last"
                    [class.border-b]="!last" class="border-gray-100 dark:border-indigo-900/15">
                  <td class="px-4 py-3 font-medium text-gray-800 dark:text-slate-200">{{ s.planNombre }}</td>
                  <td class="px-4 py-3"><app-badge-estado [estado]="s.estado" /></td>
                  <td class="px-4 py-3 text-gray-500 dark:text-slate-400">{{ s.fechaInicio | date:'dd/MM/yyyy' }}</td>
                  <td class="px-4 py-3 text-gray-700 dark:text-slate-300">{{ s.planMonto | monedaArg:s.planMoneda }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

      </ng-container>
    </div>
  `,
})
export class DashboardComponent implements OnInit {
  private readonly auth      = inject(AuthService);
  private readonly suscSvc   = inject(SuscripcionService);
  private readonly mpSvc     = inject(MpService);
  private readonly dialog    = inject(MatDialog);
  private readonly snackBar  = inject(MatSnackBar);

  readonly cargando           = signal(false);
  readonly procesando         = signal(false);
  readonly todasSuscripciones = signal<MpSuscripcion[]>([]);
  readonly pagosUnicos        = signal<MpPagoUnico[]>([]);
  readonly planes             = signal<MpPlan[]>([]);
  readonly planSeleccionado   = signal<MpPlan | null>(null);
  readonly mostrarHistorial   = signal(false);
  diasCobro: Record<number, number | null> = {};

  private readonly ESTADOS_ACTIVOS = new Set(['authorized', 'paused', 'pending']);

  readonly suscripcionActiva = computed(() =>
    this.todasSuscripciones().find(s => this.ESTADOS_ACTIVOS.has(s.estado)) ?? null
  );

  readonly historial = computed(() =>
    this.todasSuscripciones().filter(s => !this.ESTADOS_ACTIVOS.has(s.estado))
  );

  readonly pagoUnicoEsteMes = computed(() => {
    const ahora = new Date();
    return this.pagosUnicos().find(p => {
      if (p.estado !== 'approved') return false;
      const fecha = new Date(p.fechaPago ?? p.fechaCreacion);
      return fecha.getFullYear() === ahora.getFullYear() && fecha.getMonth() === ahora.getMonth();
    }) ?? null;
  });

  ngOnInit(): void {
    this.cargarDatos();
  }

  private cargarDatos(): void {
    const clienteId = this.auth.getClienteId();
    if (!clienteId) { this.cargarPlanes(); return; }

    this.cargando.set(true);
    this.suscSvc.getMisSuscripciones(clienteId).subscribe({
      next: suscs => {
        this.todasSuscripciones.set(suscs);
        this.suscSvc.getMisPagosUnicos(clienteId).subscribe({
          next: pagos => this.pagosUnicos.set(pagos),
          error: () => {},
        });
        this.cargarPlanes();
      },
      error: () => this.cargarPlanes(),
    });
  }

  private cargarPlanes(): void {
    this.suscSvc.getPlanesActivos().subscribe({
      next: planes => { this.planes.set(planes); this.cargando.set(false); },
      error: () => { this.mostrarError('No se pudieron cargar los planes.'); this.cargando.set(false); },
    });
  }

  seleccionarPlan(plan: MpPlan): void {
    this.planSeleccionado.set(plan);
  }

  suscribirse(plan: MpPlan): void {
    const usuario   = this.auth.usuario();
    const clienteId = this.auth.getClienteId();
    if (!usuario || !clienteId) return;

    this.procesando.set(true);
    const diaCobro = this.diasCobro[plan.mpPlanId];
    this.suscSvc.crearConRedirect({
      clienteId:  clienteId,
      mpPlanId:   plan.mpPlanId,
      payerEmail: usuario.email,
      ...(diaCobro ? { diaCobro } : {}),
    }).subscribe({
      next: res => this.mpSvc.redirectToInitPoint(res.initPoint),
      error: err => { this.mostrarError(err.message); this.procesando.set(false); },
    });
  }

  irACompletarSuscripcion(): void {
    const initPoint = this.suscripcionActiva()?.initPoint;
    if (initPoint) this.mpSvc.redirectToInitPoint(initPoint);
  }

  pagarEsteMes(): void {
    const susc    = this.suscripcionActiva();
    const usuario = this.auth.usuario();
    if (!susc || !usuario) return;

    this.procesando.set(true);
    this.suscSvc.crearPagoUnico({
      mpPlanId:   susc.mpPlanId,
      payerEmail: usuario.email,
    }).subscribe({
      next: res => this.mpSvc.redirectToInitPoint(res.initPoint),
      error: err => { this.mostrarError(err.message); this.procesando.set(false); },
    });
  }

  abrirModalCancelar(): void {
    const data: ModalConfirmacionData = {
      titulo:         'Cancelar suscripción',
      mensaje:        'Esta acción no se puede deshacer. Indicá el motivo de la cancelación.',
      labelConfirmar: 'Cancelar suscripción',
      labelCancelar:  'Volver',
      tipo:           'danger',
      campoTexto: { label: 'Motivo', placeholder: 'Ej: Ya no necesito el servicio', requerido: true },
    };

    this.dialog
      .open<ModalConfirmacionComponent, ModalConfirmacionData, ModalConfirmacionResult>(
        ModalConfirmacionComponent, { data, width: '480px' },
      )
      .afterClosed()
      .subscribe(result => {
        if (result?.confirmado) this.confirmarCancelacion(result.texto);
      });
  }

  private confirmarCancelacion(motivo?: string): void {
    const susc = this.suscripcionActiva();
    if (!susc) return;

    this.procesando.set(true);
    this.suscSvc
      .cancelarSuscripcion({ mpSuscripcionId: susc.mpSuscripcionId, motivo })
      .subscribe({
        next: () => {
          this.snackBar.open('Suscripción cancelada correctamente.', 'OK', { duration: 4000 });
          this.cargarDatos();
          this.procesando.set(false);
        },
        error: err => { this.mostrarError(err.message); this.procesando.set(false); },
      });
  }

  private mostrarError(msg: string): void {
    this.snackBar.open(msg, 'OK', { duration: 5000, panelClass: ['snack-error'] });
  }
}
