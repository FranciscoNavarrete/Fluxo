import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

function passwordsIguales(ctrl: AbstractControl): ValidationErrors | null {
  const p = ctrl.get('nueva')?.value;
  const c = ctrl.get('confirmar')?.value;
  return p && c && p !== c ? { noCoinciden: true } : null;
}

@Component({
  selector: 'app-perfil',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatIconModule, LoadingSpinnerComponent],
  template: `
    <div class="max-w-2xl mx-auto">
      <h1 class="text-2xl font-semibold text-gray-800 dark:text-slate-100 mb-6">Mi perfil</h1>

      <app-loading-spinner *ngIf="cargando()" />

      <ng-container *ngIf="!cargando()">

        <!-- Datos personales -->
        <div class="bg-white dark:bg-[#0b1628] rounded-xl border border-gray-200 dark:border-indigo-900/40 shadow-sm overflow-hidden mb-6">
          <div class="px-6 py-4 border-b border-gray-100 dark:border-indigo-900/30 flex items-center gap-2">
            <mat-icon class="text-indigo-500 dark:text-indigo-400">person</mat-icon>
            <span class="font-semibold text-gray-800 dark:text-slate-100">Datos personales</span>
          </div>

          <div *ngIf="errorPerfil()" class="mx-6 mt-4 px-4 py-3 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/40 flex items-start gap-2">
            <mat-icon class="text-red-500 shrink-0 text-[18px] mt-0.5">error_outline</mat-icon>
            <p class="text-sm text-red-700 dark:text-red-400">{{ errorPerfil() }}</p>
          </div>

          <form [formGroup]="formPerfil" (ngSubmit)="guardarPerfil()" class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Nombre *</label>
                <input formControlName="nombre" placeholder="Juan"
                       class="w-full border border-gray-300 dark:border-indigo-900/40 rounded-lg px-3 py-2.5 text-sm
                              bg-white dark:bg-[#0a1628] text-gray-900 dark:text-slate-100
                              placeholder-gray-400 dark:placeholder-slate-600
                              focus:outline-none focus:border-indigo-500 dark:focus:border-indigo-400 transition-colors" />
                <p *ngIf="formPerfil.get('nombre')?.invalid && formPerfil.get('nombre')?.touched"
                   class="text-xs text-red-600 dark:text-red-400 mt-1">Requerido</p>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Apellido</label>
                <input formControlName="apellido" placeholder="Pérez"
                       class="w-full border border-gray-300 dark:border-indigo-900/40 rounded-lg px-3 py-2.5 text-sm
                              bg-white dark:bg-[#0a1628] text-gray-900 dark:text-slate-100
                              placeholder-gray-400 dark:placeholder-slate-600
                              focus:outline-none focus:border-indigo-500 dark:focus:border-indigo-400 transition-colors" />
              </div>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Email</label>
              <input [value]="email()" disabled
                     class="w-full border border-gray-200 dark:border-indigo-900/20 rounded-lg px-3 py-2.5 text-sm
                            bg-gray-50 dark:bg-[#060d1c] text-gray-400 dark:text-slate-500 cursor-not-allowed" />
              <p class="text-xs text-gray-400 dark:text-slate-500 mt-1">El email no se puede modificar.</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Teléfono</label>
              <input formControlName="telefono" placeholder="+54 11 1234-5678" type="tel"
                     class="w-full border border-gray-300 dark:border-indigo-900/40 rounded-lg px-3 py-2.5 text-sm
                            bg-white dark:bg-[#0a1628] text-gray-900 dark:text-slate-100
                            placeholder-gray-400 dark:placeholder-slate-600
                            focus:outline-none focus:border-indigo-500 dark:focus:border-indigo-400 transition-colors" />
            </div>

            <div class="flex justify-end pt-2">
              <button type="submit" mat-flat-button color="primary" [disabled]="guardandoPerfil()">
                {{ guardandoPerfil() ? 'Guardando...' : 'Guardar cambios' }}
              </button>
            </div>
          </form>
        </div>

        <!-- Cambiar contraseña -->
        <div class="bg-white dark:bg-[#0b1628] rounded-xl border border-gray-200 dark:border-indigo-900/40 shadow-sm overflow-hidden">
          <div class="px-6 py-4 border-b border-gray-100 dark:border-indigo-900/30 flex items-center gap-2">
            <mat-icon class="text-indigo-500 dark:text-indigo-400">lock</mat-icon>
            <span class="font-semibold text-gray-800 dark:text-slate-100">Cambiar contraseña</span>
          </div>

          <div *ngIf="errorPass()" class="mx-6 mt-4 px-4 py-3 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/40 flex items-start gap-2">
            <mat-icon class="text-red-500 shrink-0 text-[18px] mt-0.5">error_outline</mat-icon>
            <p class="text-sm text-red-700 dark:text-red-400">{{ errorPass() }}</p>
          </div>

          <form [formGroup]="formPass" (ngSubmit)="cambiarPassword()" class="p-6 space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Nueva contraseña</label>
              <input formControlName="nueva" type="password" placeholder="Mínimo 8 caracteres"
                     class="w-full border border-gray-300 dark:border-indigo-900/40 rounded-lg px-3 py-2.5 text-sm
                            bg-white dark:bg-[#0a1628] text-gray-900 dark:text-slate-100
                            placeholder-gray-400 dark:placeholder-slate-600
                            focus:outline-none focus:border-indigo-500 dark:focus:border-indigo-400 transition-colors" />
              <p *ngIf="formPass.get('nueva')?.invalid && formPass.get('nueva')?.touched"
                 class="text-xs text-red-600 dark:text-red-400 mt-1">Mínimo 8 caracteres</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Confirmar contraseña</label>
              <input formControlName="confirmar" type="password" placeholder="Repetí la contraseña"
                     class="w-full border border-gray-300 dark:border-indigo-900/40 rounded-lg px-3 py-2.5 text-sm
                            bg-white dark:bg-[#0a1628] text-gray-900 dark:text-slate-100
                            placeholder-gray-400 dark:placeholder-slate-600
                            focus:outline-none focus:border-indigo-500 dark:focus:border-indigo-400 transition-colors" />
              <p *ngIf="formPass.errors?.['noCoinciden'] && formPass.get('confirmar')?.touched"
                 class="text-xs text-red-600 dark:text-red-400 mt-1">Las contraseñas no coinciden</p>
            </div>
            <div class="flex justify-end pt-2">
              <button type="submit" mat-flat-button color="primary" [disabled]="guardandoPass()">
                {{ guardandoPass() ? 'Cambiando...' : 'Cambiar contraseña' }}
              </button>
            </div>
          </form>
        </div>

      </ng-container>
    </div>
  `,
})
export class PerfilComponent implements OnInit {
  private readonly auth     = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb       = inject(FormBuilder);

  readonly cargando       = signal(true);
  readonly email          = signal('');
  readonly guardandoPerfil = signal(false);
  readonly guardandoPass  = signal(false);
  readonly errorPerfil    = signal('');
  readonly errorPass      = signal('');

  formPerfil = this.fb.group({
    nombre:   ['', Validators.required],
    apellido: [''],
    telefono: [''],
  });

  formPass = this.fb.group({
    nueva:     ['', [Validators.required, Validators.minLength(8)]],
    confirmar: ['', Validators.required],
  }, { validators: passwordsIguales });

  ngOnInit(): void {
    this.auth.obtenerPerfil().subscribe({
      next: perfil => {
        this.email.set(perfil.email);
        this.formPerfil.patchValue({
          nombre:   perfil.nombre,
          apellido: perfil.apellido ?? '',
          telefono: perfil.telefono ?? '',
        });
        this.cargando.set(false);
      },
      error: () => { this.cargando.set(false); },
    });
  }

  guardarPerfil(): void {
    if (this.formPerfil.invalid) { this.formPerfil.markAllAsTouched(); return; }
    this.errorPerfil.set('');
    this.guardandoPerfil.set(true);

    const v = this.formPerfil.value;
    this.auth.actualizarPerfil({ nombre: v.nombre!, apellido: v.apellido || undefined, telefono: v.telefono || undefined }).subscribe({
      next: () => {
        this.snackBar.open('Datos actualizados correctamente.', 'OK', { duration: 3000 });
        this.guardandoPerfil.set(false);
      },
      error: err => {
        this.errorPerfil.set(err?.message ?? 'Error al guardar.');
        this.guardandoPerfil.set(false);
      },
    });
  }

  cambiarPassword(): void {
    if (this.formPass.invalid) { this.formPass.markAllAsTouched(); return; }
    this.errorPass.set('');
    this.guardandoPass.set(true);

    this.auth.cambiarPassword(this.formPass.value.nueva!).subscribe({
      next: () => {
        this.snackBar.open('Contraseña cambiada correctamente.', 'OK', { duration: 3000 });
        this.formPass.reset();
        this.guardandoPass.set(false);
      },
      error: err => {
        this.errorPass.set(err?.message ?? 'Error al cambiar la contraseña.');
        this.guardandoPass.set(false);
      },
    });
  }
}
