import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../../core/services/api.service';
import { SuscripcionService } from '../../../core/services/suscripcion.service';
import { MpPlan } from '../../../shared/models';
import { MonedaArgPipe } from '../../../shared/pipes/moneda-arg.pipe';

interface CrearClienteResult {
  clienteId: number;
  usuarioId: number;
  email: string;
  passwordTemporal: string;
  initPoint?: string;
  mpSuscripcionId?: number;
}

@Component({
  selector: 'app-crear-cliente',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MonedaArgPipe,
  ],
  styles: [`
    .fi { width:100%; border:1px solid #d1d5db; border-radius:8px; padding:9px 12px; font-size:14px; color:#111827; background:#fff; outline:none; box-sizing:border-box; font-family:Roboto,sans-serif; }
    :host-context(.dark) .fi { border-color:#4b5563; background:#374151; color:#f3f4f6; }
  `],
  template: `
    <div class="max-w-2xl">

      <!-- Header -->
      <div class="mb-6">
        <h1 class="text-2xl font-semibold text-gray-800 dark:text-gray-100">Nuevo cliente</h1>
        <p class="text-sm text-gray-400 mt-0.5">Creá un cliente, su cuenta de acceso y suscripción en un paso.</p>
      </div>

      <!-- Resultado exitoso -->
      <div *ngIf="resultado()" class="bg-white dark:bg-gray-800 rounded-xl border border-green-200 dark:border-green-700 shadow-sm overflow-hidden mb-6">
        <div class="bg-green-50 px-6 py-4 border-b border-green-200 flex items-center gap-2">
          <mat-icon class="text-green-600">check_circle</mat-icon>
          <span class="font-semibold text-green-800">Cliente creado correctamente</span>
        </div>
        <div class="p-6 space-y-4">

          <div class="grid grid-cols-2 gap-4 text-sm">
            <div>
              <p class="text-gray-400">Email de acceso</p>
              <p class="font-medium text-gray-800">{{ resultado()!.email }}</p>
            </div>
            <div>
              <p class="text-gray-400">ID Cliente</p>
              <p class="font-medium text-gray-800">#{{ resultado()!.clienteId }}</p>
            </div>
          </div>

          <!-- Aviso cambio de contraseña -->
          <div class="flex items-start gap-2 px-4 py-3 rounded-lg bg-indigo-50 dark:bg-indigo-900/20 border border-indigo-200 dark:border-indigo-700/40">
            <mat-icon class="text-indigo-500 shrink-0 text-[18px] mt-0.5">info</mat-icon>
            <p class="text-sm text-indigo-700 dark:text-indigo-300">
              El cliente deberá <strong>cambiar su contraseña</strong> la primera vez que inicie sesión.
            </p>
          </div>

          <!-- Password temporal — mostrar solo una vez -->
          <div class="rounded-lg bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700/40 p-4">
            <p class="text-xs font-semibold text-amber-700 dark:text-amber-400 mb-1">Contraseña temporal — copiar ahora, no se vuelve a mostrar</p>
            <div class="flex items-center gap-2">
              <code class="flex-1 text-sm font-mono bg-white border border-amber-200 rounded px-3 py-2 text-gray-800">
                {{ resultado()!.passwordTemporal }}
              </code>
              <button mat-icon-button (click)="copiar(resultado()!.passwordTemporal)" title="Copiar">
                <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px">content_copy</mat-icon>
              </button>
            </div>
          </div>

          <!-- Link de suscripción MP -->
          <div *ngIf="resultado()!.initPoint" class="rounded-lg bg-blue-50 border border-blue-200 p-4">
            <p class="text-xs font-semibold text-blue-700 mb-1">Link de pago Mercado Pago — enviárselo al cliente</p>
            <div class="flex items-center gap-2">
              <input readonly [value]="resultado()!.initPoint"
                     class="flex-1 text-xs font-mono bg-white border border-blue-200 rounded px-3 py-2 text-gray-700 min-w-0"/>
              <button mat-icon-button (click)="copiar(resultado()!.initPoint!)" title="Copiar">
                <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px">content_copy</mat-icon>
              </button>
              <a [href]="resultado()!.initPoint" target="_blank" mat-icon-button title="Abrir">
                <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px">open_in_new</mat-icon>
              </a>
            </div>
          </div>

          <div *ngIf="!resultado()!.initPoint" class="rounded-lg bg-gray-50 border border-gray-200 p-3">
            <p class="text-xs text-gray-500">No se seleccionó un plan — podés crear la suscripción desde la tabla de suscripciones.</p>
          </div>

          <button mat-stroked-button (click)="reiniciar()" class="w-full">
            <mat-icon>add</mat-icon>
            Crear otro cliente
          </button>
        </div>
      </div>

      <!-- Formulario -->
      <div *ngIf="!resultado()" class="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">

        <!-- Error -->
        <div *ngIf="errorMsg()" class="px-6 pt-4">
          <div class="px-4 py-3 rounded-lg bg-red-50 border border-red-200 flex items-start gap-2">
            <mat-icon class="text-red-500 text-[18px] mt-0.5 shrink-0">error_outline</mat-icon>
            <p class="text-sm text-red-700">{{ errorMsg() }}</p>
          </div>
        </div>

        <form [formGroup]="form" (ngSubmit)="crear()" class="p-6 space-y-4">

          <!-- Nombre y Apellido -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Nombre *</label>
              <input type="text" formControlName="nombre" placeholder="Juan"
                     style="width:100%;border:1px solid #d1d5db;border-radius:8px;padding:9px 12px;font-size:14px;box-sizing:border-box;outline:none"/>
              <p *ngIf="form.get('nombre')?.invalid && form.get('nombre')?.touched"
                 class="text-xs text-red-600 mt-1">Requerido</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Apellido *</label>
              <input type="text" formControlName="apellido" placeholder="Pérez"
                     style="width:100%;border:1px solid #d1d5db;border-radius:8px;padding:9px 12px;font-size:14px;box-sizing:border-box;outline:none"/>
              <p *ngIf="form.get('apellido')?.invalid && form.get('apellido')?.touched"
                 class="text-xs text-red-600 mt-1">Requerido</p>
            </div>
          </div>

          <!-- Email -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Email *</label>
            <input type="email" formControlName="email" placeholder="cliente@email.com"
                   style="width:100%;border:1px solid #d1d5db;border-radius:8px;padding:9px 12px;font-size:14px;box-sizing:border-box;outline:none"/>
            <p *ngIf="form.get('email')?.invalid && form.get('email')?.touched"
               class="text-xs text-red-600 mt-1">Email válido requerido</p>
          </div>

          <!-- Teléfono -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Teléfono (opcional)</label>
            <input type="tel" formControlName="telefono" placeholder="+54 11 1234-5678"
                   style="width:100%;border:1px solid #d1d5db;border-radius:8px;padding:9px 12px;font-size:14px;box-sizing:border-box;outline:none"/>
          </div>

          <!-- Plan (opcional) -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Plan (opcional)</label>
            <select formControlName="mpPlanId"
                    style="width:100%;border:1px solid #d1d5db;border-radius:8px;padding:9px 12px;font-size:14px;background:#fff;box-sizing:border-box;outline:none">
              <option value="">Sin suscripción por ahora</option>
              <option *ngFor="let p of planes()" [value]="p.mpPlanId">
                {{ p.nombre }} — {{ p.monto | monedaArg:p.moneda }}
              </option>
            </select>
            <p class="text-xs text-gray-400 mt-1">Si elegís un plan se genera el link de pago de Mercado Pago.</p>
          </div>

          <!-- Día de cobro (solo si se eligió un plan) -->
          <div *ngIf="form.get('mpPlanId')?.value">
            <label class="block text-sm font-medium text-gray-700 mb-1">Día de cobro (opcional)</label>
            <input type="number" formControlName="diaCobro" placeholder="Ej: 10"
                   min="1" max="31"
                   style="width:100%;border:1px solid #d1d5db;border-radius:8px;padding:9px 12px;font-size:14px;box-sizing:border-box;outline:none"/>
            <p class="text-xs text-gray-400 mt-1">Día del mes en que se cobra (1-31). Si el mes no tiene ese día se usa el último día disponible.</p>
          </div>

          <div class="pt-2 border-t border-gray-100">
            <button mat-flat-button color="primary" type="submit"
                    class="w-full h-11" [disabled]="form.invalid || cargando()">
              <mat-spinner *ngIf="cargando()" diameter="18" class="inline-block mr-2"></mat-spinner>
              {{ cargando() ? 'Creando...' : 'Crear cliente' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
})
export class CrearClienteComponent {
  private readonly api     = inject(ApiService);
  private readonly suscSvc = inject(SuscripcionService);
  private readonly fb      = inject(FormBuilder);

  readonly cargando  = signal(false);
  readonly errorMsg  = signal('');
  readonly resultado = signal<CrearClienteResult | null>(null);
  readonly planes    = signal<MpPlan[]>([]);

  form = this.fb.group({
    nombre:   ['', Validators.required],
    apellido: ['', Validators.required],
    email:    ['', [Validators.required, Validators.email]],
    telefono: [''],
    mpPlanId: [''],
    diaCobro: [null as number | null],
  });

  constructor() {
    this.suscSvc.getPlanesActivos().subscribe({
      next: p => this.planes.set(p.filter(x => x.activo)),
      error: () => {},
    });
  }

  crear(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.errorMsg.set('');
    this.cargando.set(true);
    const v = this.form.value;
    const body: Record<string, unknown> = {
      nombre:   v.nombre,
      apellido: v.apellido,
      email:    v.email,
      telefono: v.telefono || null,
    };
    if (v.mpPlanId) body['mpPlanId'] = Number(v.mpPlanId);
    if (v.mpPlanId && v.diaCobro) body['diaCobro'] = Number(v.diaCobro);

    this.api.post<CrearClienteResult>('admin/clientes', body).subscribe({
      next:  res => { this.resultado.set(res); this.cargando.set(false); },
      error: err => { this.errorMsg.set(err.message ?? 'Error al crear el cliente'); this.cargando.set(false); },
    });
  }

  copiar(texto: string): void {
    navigator.clipboard.writeText(texto);
  }

  reiniciar(): void {
    this.resultado.set(null);
    this.errorMsg.set('');
    this.form.reset();
  }
}
