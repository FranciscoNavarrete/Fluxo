import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { SuscripcionService } from '../../../core/services/suscripcion.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import {
  ModalConfirmacionComponent,
  ModalConfirmacionData,
  ModalConfirmacionResult,
} from '../../../shared/components/modal-confirmacion/modal-confirmacion.component';
import { MonedaArgPipe } from '../../../shared/pipes/moneda-arg.pipe';
import { MpPlan } from '../../../shared/models';


@Component({
  selector: 'app-planes',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    LoadingSpinnerComponent,
    MonedaArgPipe,
  ],
  styles: [`
    .fi {
      width: 100%; border: 1px solid #d1d5db; border-radius: 8px;
      padding: 9px 12px; font-size: 14px; box-sizing: border-box;
      outline: none; background: #fff; color: #111827;
      font-family: Roboto, "Helvetica Neue", sans-serif;
      transition: border-color 0.15s;
    }
    .fi:focus { border-color: #6366f1; }
    :host-context(.dark) .fi {
      background: #0a1628; border-color: rgba(99,102,241,0.3);
      color: #e2e8f0;
    }
    :host-context(.dark) .fi:focus { border-color: rgba(99,102,241,0.7); }
    :host-context(.dark) .fi::placeholder { color: #475569; }
    :host-context(.dark) option { background: #0f1e3d; color: #e2e8f0; }
  `],
  template: `
    <div class="max-w-4xl mx-auto">

      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-semibold text-gray-800 dark:text-slate-100">Planes</h1>
        <button mat-flat-button color="primary" (click)="abrirFormulario()">
          <mat-icon>add</mat-icon> Nuevo plan
        </button>
      </div>

      <app-loading-spinner [visible]="cargando()" mensaje="Cargando planes..." />

      <!-- Formulario crear / editar plan -->
      <div *ngIf="mostrarFormulario()"
           class="bg-white dark:bg-[#0b1628] rounded-xl border border-gray-200 dark:border-indigo-900/40 shadow-sm overflow-hidden mb-6">

        <div class="bg-indigo-50 dark:bg-indigo-900/20 px-6 py-4 border-b border-gray-200 dark:border-indigo-900/30 flex items-center gap-2">
          <mat-icon class="text-indigo-600 dark:text-indigo-400">{{ planEditando() ? 'edit' : 'add_circle' }}</mat-icon>
          <span class="font-semibold text-gray-800 dark:text-slate-100">{{ planEditando() ? 'Editar plan' : 'Nuevo plan' }}</span>
        </div>

        <div *ngIf="errorForm()" class="px-6 pt-4">
          <div class="px-4 py-3 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/40 flex items-start gap-2">
            <mat-icon class="text-red-500 text-[18px] mt-0.5 shrink-0">error_outline</mat-icon>
            <p class="text-sm text-red-700 dark:text-red-400">{{ errorForm() }}</p>
          </div>
        </div>

        <form [formGroup]="form" (ngSubmit)="guardar()" class="p-6 space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Nombre *</label>
              <input formControlName="nombre" placeholder="Plan Mensual" class="fi" />
              <p *ngIf="form.get('nombre')?.invalid && form.get('nombre')?.touched"
                 class="text-xs text-red-600 dark:text-red-400 mt-1">Requerido</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Descripción</label>
              <input formControlName="descripcion" placeholder="Descripción opcional" class="fi" />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Monto *</label>
              <input type="number" formControlName="monto" placeholder="5000" min="0" class="fi" />
              <p *ngIf="form.get('monto')?.invalid && form.get('monto')?.touched"
                 class="text-xs text-red-600 dark:text-red-400 mt-1">Requerido, mayor a 0</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Moneda *</label>
              <select formControlName="moneda" class="fi">
                <option value="ARS">ARS — Peso argentino</option>
                <option value="USD">USD — Dólar</option>
              </select>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Tipo de frecuencia *</label>
              <select formControlName="tipoFrecuencia" class="fi">
                <option value="months">Meses</option>
                <option value="days">Días</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Cada cuántos *</label>
              <input type="number" formControlName="frecuencia" placeholder="1" min="1" class="fi" />
              <p *ngIf="form.get('frecuencia')?.invalid && form.get('frecuencia')?.touched"
                 class="text-xs text-red-600 dark:text-red-400 mt-1">Requerido, mínimo 1</p>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Días gratis (opcional)</label>
              <input type="number" formControlName="diasGratis" placeholder="0" min="0" class="fi" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Cantidad de cobros (opcional)</label>
              <input type="number" formControlName="repeticiones" placeholder="Vacío = sin límite" min="1" class="fi" />
              <p class="text-xs text-gray-400 dark:text-slate-500 mt-1">Ej: 12 = plan anual</p>
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Monto del primer cobro (opcional)</label>
            <input type="number" formControlName="montoPrimerCobro" placeholder="Vacío = igual al monto mensual" min="1" class="fi" />
            <p class="text-xs text-gray-400 dark:text-slate-500 mt-1">
              Ej: alta + primer mes. El primer cobro sale por este monto y después baja solo al monto mensual.
            </p>
          </div>

          <div class="flex justify-end gap-3 pt-2 border-t border-gray-100 dark:border-indigo-900/20">
            <button type="button" mat-stroked-button (click)="cancelarFormulario()">Cancelar</button>
            <button type="submit" mat-flat-button color="primary" [disabled]="guardando()">
              {{ guardando() ? 'Guardando...' : (planEditando() ? 'Guardar cambios' : 'Crear plan') }}
            </button>
          </div>
        </form>
      </div>

      <!-- Listado de planes -->
      <div *ngIf="!cargando()" class="grid grid-cols-1 md:grid-cols-2 gap-5">
        <mat-card *ngFor="let plan of planes()"
          [class.opacity-50]="!plan.activo"
          class="transition-opacity">
          <mat-card-content class="p-5">
            <div class="flex items-start justify-between">
              <div class="flex-1">
                <div class="flex items-center gap-2 mb-1">
                  <h3 class="font-semibold text-gray-800 dark:text-slate-100">{{ plan.nombre }}</h3>
                  <span class="text-[10px] px-2 py-0.5 rounded-full font-medium"
                    [class.bg-green-100]="plan.activo" [class.text-green-700]="plan.activo"
                    [class.bg-gray-100]="!plan.activo" [class.text-gray-500]="!plan.activo">
                    {{ plan.activo ? 'Activo' : 'Inactivo' }}
                  </span>
                </div>
                <p class="text-sm text-gray-500 dark:text-slate-400 mb-3">{{ plan.descripcion }}</p>

                <dl class="grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
                  <div>
                    <dt class="text-gray-400 dark:text-slate-500 text-xs">Monto</dt>
                    <dd class="font-semibold text-indigo-600 dark:text-indigo-400">{{ plan.monto | monedaArg:plan.moneda }}</dd>
                  </div>
                  <div>
                    <dt class="text-gray-400 dark:text-slate-500 text-xs">Frecuencia</dt>
                    <dd class="font-medium text-gray-700 dark:text-slate-300">
                      Cada {{ plan.frecuencia }} {{ plan.tipoFrecuencia === 'months' ? 'mes/es' : 'día/s' }}
                    </dd>
                  </div>
                  <div>
                    <dt class="text-gray-400 dark:text-slate-500 text-xs">Días gratis</dt>
                    <dd class="font-medium text-gray-700 dark:text-slate-300">{{ plan.diasGratis || 0 }}</dd>
                  </div>
                  <div>
                    <dt class="text-gray-400 dark:text-slate-500 text-xs">Moneda</dt>
                    <dd class="font-medium text-gray-700 dark:text-slate-300">{{ plan.moneda }}</dd>
                  </div>
                  <div *ngIf="plan.montoPrimerCobro">
                    <dt class="text-gray-400 dark:text-slate-500 text-xs">Primer cobro</dt>
                    <dd class="font-medium text-gray-700 dark:text-slate-300">
                      {{ plan.montoPrimerCobro | number:'1.0-2' }} {{ plan.moneda }}
                    </dd>
                  </div>
                  <div>
                    <dt class="text-gray-400 dark:text-slate-500 text-xs">Cobros</dt>
                    <dd class="font-medium text-gray-700 dark:text-slate-300">
                      {{ plan.repeticiones ? plan.repeticiones + ' cobros' : 'Sin límite' }}
                    </dd>
                  </div>
                </dl>
              </div>

              <!-- Acciones -->
              <div class="ml-4 flex flex-col items-center gap-3">
                <button mat-icon-button title="Editar" (click)="abrirEditar(plan)"
                        class="text-gray-400 dark:text-slate-500 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors">
                  <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px">edit</mat-icon>
                </button>
                <button mat-icon-button title="Eliminar" (click)="confirmarEliminar(plan)"
                        [disabled]="procesando() === plan.mpPlanId"
                        class="text-gray-400 dark:text-slate-500 hover:text-red-500 transition-colors">
                  <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px">delete</mat-icon>
                </button>
                <mat-slide-toggle
                  [checked]="plan.activo"
                  [disabled]="procesando() === plan.mpPlanId"
                  (change)="confirmarToggle(plan, $event.checked)"
                  color="primary" />
                <span class="text-[10px] text-gray-400 dark:text-slate-500">
                  {{ procesando() === plan.mpPlanId ? '...' : (plan.activo ? 'Desactivar' : 'Activar') }}
                </span>
              </div>
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <div *ngIf="!cargando() && !planes().length" class="text-center text-gray-400 dark:text-slate-600 py-16 text-sm">
        No hay planes configurados. Creá el primero con el botón de arriba.
      </div>
    </div>
  `,
})
export class PlanesComponent implements OnInit {
  private readonly suscSvc  = inject(SuscripcionService);
  private readonly dialog   = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb       = inject(FormBuilder);

  readonly cargando          = signal(false);
  readonly guardando         = signal(false);
  readonly planes            = signal<MpPlan[]>([]);
  readonly procesando        = signal<number | null>(null);
  readonly mostrarFormulario = signal(false);
  readonly errorForm         = signal('');
  readonly planEditando      = signal<MpPlan | null>(null);

  form = this.fb.group({
    nombre:        ['', Validators.required],
    descripcion:   [''],
    monto:         [null as number | null, [Validators.required, Validators.min(1)]],
    moneda:        ['ARS', Validators.required],
    tipoFrecuencia: ['months', Validators.required],
    frecuencia:    [1, [Validators.required, Validators.min(1)]],
    diasGratis:    [0],
    repeticiones:  [null as number | null],
    montoPrimerCobro: [null as number | null],
  });

  ngOnInit(): void {
    this.cargar();
  }

  abrirFormulario(): void {
    this.planEditando.set(null);
    this.form.reset({ moneda: 'ARS', tipoFrecuencia: 'months', frecuencia: 1, diasGratis: 0, repeticiones: null, montoPrimerCobro: null });
    this.errorForm.set('');
    this.mostrarFormulario.set(true);
    setTimeout(() => window.scrollTo({ top: 0, behavior: 'smooth' }), 50);
  }

  abrirEditar(plan: MpPlan): void {
    this.planEditando.set(plan);
    this.form.reset({
      nombre:         plan.nombre,
      descripcion:    plan.descripcion ?? '',
      monto:          plan.monto,
      moneda:         plan.moneda,
      tipoFrecuencia: plan.tipoFrecuencia,
      frecuencia:     plan.frecuencia,
      diasGratis:     plan.diasGratis,
      repeticiones:   plan.repeticiones ?? null,
      montoPrimerCobro: plan.montoPrimerCobro ?? null,
    });
    this.errorForm.set('');
    this.mostrarFormulario.set(true);
    setTimeout(() => window.scrollTo({ top: 0, behavior: 'smooth' }), 50);
  }

  cancelarFormulario(): void {
    this.mostrarFormulario.set(false);
    this.planEditando.set(null);
    this.errorForm.set('');
  }

  guardar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.errorForm.set('');
    this.guardando.set(true);
    const v = this.form.value;
    const editando = this.planEditando();

    if (editando) {
      this.suscSvc.editarPlan({
        mpPlanId:      editando.mpPlanId,
        nombre:        v.nombre!,
        descripcion:   v.descripcion || undefined,
        monto:         Number(v.monto),
        moneda:        v.moneda!,
        tipoFrecuencia: v.tipoFrecuencia!,
        frecuencia:    Number(v.frecuencia),
        diasGratis:    Number(v.diasGratis ?? 0),
        repeticiones:  v.repeticiones ? Number(v.repeticiones) : null,
        montoPrimerCobro: v.montoPrimerCobro ? Number(v.montoPrimerCobro) : null,
        activo:        editando.activo,
      }).subscribe({
        next: plan => {
          this.planes.update(lista => lista.map(p => p.mpPlanId === plan.mpPlanId ? plan : p));
          this.snackBar.open(`Plan "${plan.nombre}" actualizado.`, 'OK', { duration: 3000, panelClass: 'snack-ok' });
          this.cancelarFormulario();
          this.guardando.set(false);
        },
        error: err => { this.errorForm.set(err.message ?? 'Error al editar el plan.'); this.guardando.set(false); },
      });
    } else {
      this.suscSvc.crearPlan({
        nombre:        v.nombre!,
        descripcion:   v.descripcion || undefined,
        monto:         Number(v.monto),
        moneda:        v.moneda!,
        tipoFrecuencia: v.tipoFrecuencia!,
        frecuencia:    Number(v.frecuencia),
        diasGratis:    Number(v.diasGratis ?? 0),
        repeticiones:  v.repeticiones ? Number(v.repeticiones) : null,
        montoPrimerCobro: v.montoPrimerCobro ? Number(v.montoPrimerCobro) : null,
      }).subscribe({
        next: plan => {
          this.planes.update(lista => [plan, ...lista]);
          this.snackBar.open(`Plan "${plan.nombre}" creado correctamente.`, 'OK', { duration: 3000, panelClass: 'snack-ok' });
          this.cancelarFormulario();
          this.guardando.set(false);
        },
        error: err => { this.errorForm.set(err.message ?? 'Error al crear el plan.'); this.guardando.set(false); },
      });
    }
  }

  confirmarEliminar(plan: MpPlan): void {
    const data: ModalConfirmacionData = {
      titulo:         'Eliminar plan',
      mensaje:        `¿Confirmás que querés eliminar el plan "${plan.nombre}"? Esta acción no se puede deshacer. Los clientes con suscripciones activas en este plan no se verán afectados.`,
      labelConfirmar: 'Eliminar',
      labelCancelar:  'Cancelar',
      tipo:           'danger',
    };

    this.dialog
      .open<ModalConfirmacionComponent, ModalConfirmacionData, ModalConfirmacionResult>(
        ModalConfirmacionComponent, { data, width: '460px' },
      )
      .afterClosed()
      .subscribe(res => {
        if (!res?.confirmado) return;
        this.ejecutarEliminar(plan);
      });
  }

  private ejecutarEliminar(plan: MpPlan): void {
    this.procesando.set(plan.mpPlanId);
    this.suscSvc.eliminarPlan(plan.mpPlanId).subscribe({
      next: () => {
        this.planes.update(lista => lista.filter(p => p.mpPlanId !== plan.mpPlanId));
        this.snackBar.open(`Plan "${plan.nombre}" eliminado.`, 'OK', { duration: 3000, panelClass: 'snack-ok' });
        this.procesando.set(null);
      },
      error: err => {
        this.snackBar.open(err.message ?? 'No se pudo eliminar el plan.', 'OK', { duration: 5000, panelClass: 'snack-error' });
        this.procesando.set(null);
      },
    });
  }

  confirmarToggle(plan: MpPlan, nuevoEstado: boolean): void {
    const data: ModalConfirmacionData = {
      titulo:         nuevoEstado ? 'Activar plan' : 'Desactivar plan',
      mensaje:        nuevoEstado
        ? `¿Confirmás que querés activar el plan "${plan.nombre}"?`
        : `¿Confirmás desactivar "${plan.nombre}"? Las suscripciones existentes continúan.`,
      labelConfirmar: nuevoEstado ? 'Activar' : 'Desactivar',
      tipo:           nuevoEstado ? 'info' : 'warning',
    };

    this.dialog
      .open<ModalConfirmacionComponent, ModalConfirmacionData, ModalConfirmacionResult>(
        ModalConfirmacionComponent, { data, width: '440px' },
      )
      .afterClosed()
      .subscribe(res => {
        if (!res?.confirmado) { this.cargar(); return; }
        this.ejecutarToggle(plan, nuevoEstado);
      });
  }

  private ejecutarToggle(plan: MpPlan, activo: boolean): void {
    this.procesando.set(plan.mpPlanId);
    this.suscSvc.togglePlan(plan, activo).subscribe({
      next: planActualizado => {
        this.planes.update(lista =>
          lista.map(p => p.mpPlanId === planActualizado.mpPlanId ? planActualizado : p),
        );
        this.snackBar.open(
          `Plan "${planActualizado.nombre}" ${activo ? 'activado' : 'desactivado'}.`,
          'OK', { duration: 3000, panelClass: 'snack-ok' },
        );
        this.procesando.set(null);
      },
      error: err => {
        this.snackBar.open(err.message, 'OK', { duration: 5000, panelClass: 'snack-error' });
        this.cargar();
        this.procesando.set(null);
      },
    });
  }

  private cargar(): void {
    this.cargando.set(true);
    this.suscSvc.getPlanes().subscribe({
      next: planes => { this.planes.set(planes); this.cargando.set(false); },
      error: err => { this.snackBar.open(err.message, 'OK', { duration: 5000, panelClass: 'snack-error' }); this.cargando.set(false); },
    });
  }
}
