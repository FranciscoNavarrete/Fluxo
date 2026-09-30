import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { SuscripcionService } from '../../../core/services/suscripcion.service';
import { BadgeEstadoComponent } from '../../../shared/components/badge-estado/badge-estado.component';
import {
  CrearSuscripcionRequest,
  CrearSuscripcionResponse,
  EstadoSuscripcion,
  FiltroSuscripciones,
  MpPlan,
  MpSuscripcion,
} from '../../../shared/models';
import { MonedaArgPipe } from '../../../shared/pipes/moneda-arg.pipe';

@Component({
  selector: 'app-suscripciones-tabla',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    BadgeEstadoComponent,
    MonedaArgPipe,
  ],
  styles: [`
    th.mat-header-cell { font-weight: 600; color: #374151; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; padding: 12px 16px; }
    td.mat-cell { font-size: 0.875rem; color: #1f2937; padding: 12px 16px; }
    tr.mat-row { border-bottom: 1px solid #f3f4f6; cursor: pointer; }
    tr.mat-row:hover { background: #eff6ff; }
    :host-context(.dark) th.mat-header-cell { color: #7c8ba6; background: rgba(8,15,30,0.5); border-bottom: 1px solid rgba(99,102,241,0.12); }
    :host-context(.dark) td.mat-cell { color: #cbd5e1; border-bottom: 1px solid rgba(255,255,255,0.04); }
    :host-context(.dark) tr.mat-row { border-bottom-color: rgba(99,102,241,0.08); }
    :host-context(.dark) tr.mat-row:hover { background: rgba(99,102,241,0.08); }
    .fi { width:100%; border:1px solid #d1d5db; border-radius:8px; padding:9px 12px; font-size:14px; color:#111827; background:#fff; outline:none; box-sizing:border-box; font-family:Roboto,sans-serif; }
    :host-context(.dark) .fi { border-color:#4b5563; background:#374151; color:#f3f4f6; }
  `],
  template: `
    <div>
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-semibold text-gray-800 dark:text-gray-100">Suscripciones</h1>
          <p class="text-sm text-gray-400 mt-0.5">{{ totalItems() }} registros</p>
        </div>
        <button mat-flat-button color="primary" (click)="mostrarModal = true; cargarPlanes()">
          <mat-icon>add</mat-icon>
          Nueva suscripción
        </button>
      </div>

      <!-- Filtros -->
      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 mb-5 shadow-sm">
        <form [formGroup]="filtrosForm" (ngSubmit)="aplicarFiltros()"
              class="flex flex-wrap gap-3 items-end">
          <mat-form-field appearance="outline" class="w-44" subscriptSizing="dynamic">
            <mat-label>Estado</mat-label>
            <mat-select formControlName="estado">
              <mat-option value="">Todos</mat-option>
              <mat-option value="authorized">Activa</mat-option>
              <mat-option value="pending">Pendiente</mat-option>
              <mat-option value="paused">Pausada</mat-option>
              <mat-option value="suspended">Suspendida</mat-option>
              <mat-option value="cancelled">Cancelada</mat-option>
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-44" subscriptSizing="dynamic">
            <mat-label>ID Cliente</mat-label>
            <input matInput type="number" formControlName="clienteId" placeholder="Ej: 42" />
          </mat-form-field>

          <div class="flex gap-2">
            <button mat-flat-button color="primary" type="submit">
              <mat-icon>search</mat-icon>
              Filtrar
            </button>
            <button mat-stroked-button type="button" (click)="limpiar()">
              <mat-icon>clear</mat-icon>
            </button>
          </div>
        </form>
      </div>

      <!-- Tabla -->
      <div class="relative bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden">
        <mat-progress-bar *ngIf="cargando()" mode="indeterminate" class="absolute top-0 left-0 right-0" />

        <div *ngIf="!cargando() && suscripciones().length === 0"
             class="flex flex-col items-center justify-center py-16 text-gray-400">
          <mat-icon class="opacity-40 text-5xl mb-2">inbox</mat-icon>
          <p class="text-sm">Sin resultados</p>
        </div>

        <div class="overflow-x-auto">
          <table mat-table [dataSource]="suscripciones()" class="w-full">

            <ng-container matColumnDef="cliente">
              <th mat-header-cell *matHeaderCellDef>Cliente</th>
              <td mat-cell *matCellDef="let row">
                <span class="font-medium text-gray-800">{{ row.clienteNombre || '—' }}</span>
                <span class="block text-xs text-gray-400">{{ row.clienteEmail }}</span>
              </td>
            </ng-container>

            <ng-container matColumnDef="plan">
              <th mat-header-cell *matHeaderCellDef>Plan</th>
              <td mat-cell *matCellDef="let row" class="text-gray-700">{{ row.planNombre }}</td>
            </ng-container>

            <ng-container matColumnDef="estado">
              <th mat-header-cell *matHeaderCellDef>Estado</th>
              <td mat-cell *matCellDef="let row">
                <div class="flex items-center gap-2">
                  <app-badge-estado [estado]="row.estado" />
                  <span *ngIf="clientesCubiertos().has(row.clienteId) && (row.estado === 'paused' || row.estado === 'suspended')"
                        class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-green-100 text-green-700">
                    <mat-icon style="font-size:10px;width:10px;height:10px;line-height:10px">check_circle</mat-icon>
                    Cubierto
                  </span>
                </div>
              </td>
            </ng-container>

            <ng-container matColumnDef="fechaInicio">
              <th mat-header-cell *matHeaderCellDef>Inicio</th>
              <td mat-cell *matCellDef="let row" class="text-gray-600">
                {{ row.fechaInicio | date:'dd/MM/yyyy' }}
              </td>
            </ng-container>

            <ng-container matColumnDef="proximoCobro">
              <th mat-header-cell *matHeaderCellDef>Próximo cobro</th>
              <td mat-cell *matCellDef="let row" class="text-gray-600">
                {{ row.proximoCobro ? (row.proximoCobro | date:'dd/MM/yyyy') : '—' }}
              </td>
            </ng-container>

            <ng-container matColumnDef="intentos">
              <th mat-header-cell *matHeaderCellDef>Reintentos</th>
              <td mat-cell *matCellDef="let row" class="text-center">
                <span [class.text-red-600]="row.intentosReintento > 2"
                      [class.font-semibold]="row.intentosReintento > 2">
                  {{ row.intentosReintento }}
                </span>
              </td>
            </ng-container>

            <ng-container matColumnDef="acciones">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let row">
                <button mat-icon-button (click)="$event.stopPropagation(); verDetalle(row)">
                  <mat-icon class="text-gray-400">chevron_right</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="columnas" class="bg-gray-50"></tr>
            <tr mat-row *matRowDef="let row; columns: columnas" (click)="verDetalle(row)"></tr>
          </table>
        </div>

        <mat-paginator
          *ngIf="totalItems() > 0"
          [length]="totalItems()"
          [pageSize]="pageSize"
          [pageSizeOptions]="[10, 20, 50]"
          [pageIndex]="paginaActual"
          (page)="onPage($event)"
          showFirstLastButtons
          class="border-t border-gray-100"
        />
      </div>
    </div>

    <!-- ───── Modal Nueva Suscripción ───── -->
    <div *ngIf="mostrarModal"
         class="fixed inset-0 z-[1000] flex items-center justify-center bg-black/50"
         (click)="cerrarModal()">

      <div class="bg-white dark:bg-gray-800 rounded-2xl w-[480px] max-w-[94vw] overflow-hidden shadow-2xl"
           (click)="$event.stopPropagation()">

        <div class="flex items-center justify-between px-6 py-5 border-b border-gray-200 dark:border-gray-700">
          <span class="text-[17px] font-semibold text-gray-900 dark:text-gray-100">Nueva Suscripción</span>
          <button mat-icon-button (click)="cerrarModal()">
            <mat-icon>close</mat-icon>
          </button>
        </div>

        <div class="px-6 py-5">
          <div *ngIf="modalResultado" class="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-700 rounded-xl p-4 mb-4">
            <p class="font-semibold text-green-700 dark:text-green-400 mb-2">✓ Suscripción creada · #{{ modalResultado.mpSuscripcionId }}</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-2">Link de pago Mercado Pago:</p>
            <div class="flex gap-2 items-center">
              <input readonly [value]="modalResultado.initPoint"
                     class="fi flex-1 text-[11px] font-mono min-w-0" />
              <button mat-icon-button (click)="copiarLink()" title="Copiar">
                <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px;">content_copy</mat-icon>
              </button>
              <a [href]="modalResultado.initPoint" target="_blank" mat-icon-button title="Abrir">
                <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px;">open_in_new</mat-icon>
              </a>
            </div>
          </div>

          <div *ngIf="modalError" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-700 rounded-lg px-4 py-3 mb-4">
            <p class="text-sm text-red-600 dark:text-red-400">{{ modalError }}</p>
          </div>

          <form *ngIf="!modalResultado" [formGroup]="crearForm" (ngSubmit)="crear()">
            <div class="mb-4">
              <label class="block text-[13px] font-medium text-gray-700 dark:text-gray-300 mb-1">Plan *</label>
              <select formControlName="mpPlanId" class="fi">
                <option value="" disabled>Seleccioná un plan</option>
                <option *ngFor="let p of planes" [value]="p.mpPlanId">
                  {{ p.nombre }} — {{ p.monto | monedaArg:p.moneda }}
                </option>
              </select>
            </div>

            <div class="mb-4">
              <label class="block text-[13px] font-medium text-gray-700 dark:text-gray-300 mb-1">ID Cliente *</label>
              <input type="number" formControlName="clienteId" placeholder="Ej: 42" class="fi" />
            </div>

            <div class="mb-4">
              <label class="block text-[13px] font-medium text-gray-700 dark:text-gray-300 mb-1">Email del cliente *</label>
              <input type="email" formControlName="payerEmail" placeholder="cliente@email.com" class="fi" />
            </div>

            <div class="mb-2">
              <label class="block text-[13px] font-medium text-gray-700 dark:text-gray-300 mb-1">Día de cobro (opcional)</label>
              <input type="number" formControlName="diaCobro" placeholder="Ej: 10" min="1" max="31" class="fi" />
              <p class="text-[11px] text-gray-400 mt-1">Día del mes en que se cobra (1-31). Si el mes no tiene ese día se usa el último disponible.</p>
            </div>
          </form>
        </div>

        <div class="flex justify-end gap-2 px-6 py-4 border-t border-gray-200 dark:border-gray-700">
          <ng-container *ngIf="!modalResultado">
            <button mat-stroked-button (click)="cerrarModal()">Cancelar</button>
            <button mat-flat-button color="primary" (click)="crear()" [disabled]="crearForm.invalid || modalCargando">
              <mat-spinner *ngIf="modalCargando" diameter="16" style="display:inline-block;margin-right:6px;"></mat-spinner>
              {{ modalCargando ? 'Creando...' : 'Crear suscripción' }}
            </button>
          </ng-container>
          <button mat-flat-button color="primary" *ngIf="modalResultado" (click)="cerrarModal()">Cerrar</button>
        </div>
      </div>
    </div>
  `,
})
export class SuscripcionesTablaComponent implements OnInit {
  private readonly suscSvc  = inject(SuscripcionService);
  private readonly router   = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb       = inject(FormBuilder);

  readonly columnas = ['cliente', 'plan', 'estado', 'fechaInicio', 'proximoCobro', 'intentos', 'acciones'];
  readonly cargando           = signal(false);
  readonly suscripciones      = signal<MpSuscripcion[]>([]);
  readonly totalItems         = signal(0);
  readonly clientesCubiertos  = signal<Set<number>>(new Set());

  pageSize     = 20;
  paginaActual = 0;
  private filtros: FiltroSuscripciones = { pagina: 1, tamanioPagina: 20 };

  filtrosForm = this.fb.group({ estado: [''], clienteId: [null as number | null] });

  // ─── Modal ──────────────────────────────────────────────────────────────────
  mostrarModal   = false;
  modalCargando  = false;
  modalError     = '';
  modalResultado: CrearSuscripcionResponse | null = null;
  planes: MpPlan[] = [];

  crearForm = this.fb.group({
    mpPlanId:   ['' as string | number, Validators.required],
    clienteId:  [null as number | null, Validators.required],
    payerEmail: ['', [Validators.required, Validators.email]],
    diaCobro:   [null as number | null],
  });

  ngOnInit(): void { this.cargar(); }

  cargarPlanes(): void {
    if (this.planes.length) return;
    this.suscSvc.getPlanesActivos().subscribe({
      next:  p  => { this.planes = p.filter(x => x.activo); },
      error: () => {},
    });
  }

  cerrarModal(): void {
    const huboCreacion = !!this.modalResultado;
    this.mostrarModal   = false;
    this.modalError     = '';
    this.modalResultado = null;
    this.modalCargando  = false;
    this.crearForm.reset({ mpPlanId: '', clienteId: null, payerEmail: '', diaCobro: null });
    if (huboCreacion) this.cargar();
  }

  crear(): void {
    if (this.crearForm.invalid) { this.crearForm.markAllAsTouched(); return; }
    this.modalError    = '';
    this.modalCargando = true;
    const v = this.crearForm.value;
    const req: CrearSuscripcionRequest = {
      clienteId:  v.clienteId!,
      mpPlanId:   Number(v.mpPlanId),
      payerEmail: v.payerEmail!,
      ...(v.diaCobro ? { diaCobro: Number(v.diaCobro) } : {}),
    };
    this.suscSvc.crearConRedirect(req).subscribe({
      next:  res => { this.modalResultado = res; this.modalCargando = false; this.cargar(); },
      error: err => { this.modalError = err.message ?? 'Error al crear'; this.modalCargando = false; },
    });
  }

  copiarLink(): void {
    if (this.modalResultado?.initPoint) navigator.clipboard.writeText(this.modalResultado.initPoint);
  }

  onPage(event: PageEvent): void {
    this.paginaActual = event.pageIndex;
    this.pageSize     = event.pageSize;
    this.filtros = { ...this.filtros, pagina: event.pageIndex + 1, tamanioPagina: event.pageSize };
    this.cargar();
  }

  aplicarFiltros(): void {
    const val = this.filtrosForm.value;
    this.paginaActual = 0;
    this.filtros = {
      pagina: 1, tamanioPagina: this.pageSize,
      estado:    (val.estado as EstadoSuscripcion) || undefined,
      clienteId: val.clienteId ?? undefined,
    };
    this.cargar();
  }

  limpiar(): void {
    this.filtrosForm.reset({ estado: '', clienteId: null });
    this.paginaActual = 0;
    this.filtros = { pagina: 1, tamanioPagina: 20 };
    this.cargar();
  }

  verDetalle(row: MpSuscripcion): void {
    this.router.navigate(['/admin/suscripciones', row.mpSuscripcionId]);
  }

  private cargar(): void {
    this.cargando.set(true);
    this.suscSvc.getSuscripciones(this.filtros).subscribe({
      next: res => {
        this.suscripciones.set(res.items);
        this.totalItems.set(res.totalItems);
        this.cargando.set(false);
        this.cargarCobertura(res.items);
      },
      error: err => {
        this.snackBar.open(err.message, 'OK', { duration: 5000 });
        this.cargando.set(false);
      },
    });
  }

  private cargarCobertura(suscs: MpSuscripcion[]): void {
    const ahora = new Date();
    const pausadas = suscs.filter(s => s.estado === 'paused' || s.estado === 'suspended');
    if (!pausadas.length) { this.clientesCubiertos.set(new Set()); return; }

    const clienteIds = [...new Set(pausadas.map(s => s.clienteId))];
    const requests = clienteIds.map(id => this.suscSvc.getMisPagosUnicos(id));

    forkJoin(requests).subscribe({
      next: resultados => {
        const cubiertos = new Set<number>();
        resultados.forEach((pagos, i) => {
          const tienePago = pagos.some(p => {
            if (p.estado !== 'approved') return false;
            const fecha = new Date(p.fechaPago ?? p.fechaCreacion);
            return fecha.getFullYear() === ahora.getFullYear() && fecha.getMonth() === ahora.getMonth();
          });
          if (tienePago) cubiertos.add(clienteIds[i]);
        });
        this.clientesCubiertos.set(cubiertos);
      },
      error: () => {},
    });
  }
}
