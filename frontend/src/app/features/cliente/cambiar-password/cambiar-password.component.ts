import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/services/auth.service';

function passwordsIguales(ctrl: AbstractControl): ValidationErrors | null {
  const p = ctrl.get('nuevaPassword')?.value;
  const c = ctrl.get('confirmar')?.value;
  return p && c && p !== c ? { noCoinciden: true } : null;
}

@Component({
  selector: 'app-cambiar-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatIconModule],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-[#080f1e] flex items-center justify-center p-4">
      <div class="w-full max-w-sm">
        <div class="bg-white dark:bg-[#0f1e3d] rounded-2xl shadow-xl border border-gray-100 dark:border-indigo-900/40 overflow-hidden">

          <!-- Header -->
          <div class="px-8 pt-8 pb-6 border-b border-gray-100 dark:border-indigo-900/30">
            <div class="w-12 h-12 rounded-xl bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center mb-4">
              <mat-icon class="text-amber-600 dark:text-amber-400">key</mat-icon>
            </div>
            <h1 class="text-xl font-bold text-gray-900 dark:text-slate-100">Cambiá tu contraseña</h1>
            <p class="text-sm text-gray-500 dark:text-slate-400 mt-1">
              Tu cuenta fue creada con una contraseña temporal. Elegí una nueva antes de continuar.
            </p>
          </div>

          <div class="px-8 py-6">
            <div *ngIf="error()" class="mb-4 px-4 py-3 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/40 flex items-start gap-2">
              <mat-icon class="text-red-500 shrink-0 text-[18px] mt-0.5">error_outline</mat-icon>
              <p class="text-sm text-red-700 dark:text-red-400">{{ error() }}</p>
            </div>

            <form [formGroup]="form" (ngSubmit)="guardar()" class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Nueva contraseña</label>
                <input formControlName="nuevaPassword" type="password" placeholder="Mínimo 8 caracteres"
                       class="w-full border border-gray-300 dark:border-indigo-900/40 rounded-lg px-3 py-2.5 text-sm
                              bg-white dark:bg-[#0a1628] text-gray-900 dark:text-slate-100
                              placeholder-gray-400 dark:placeholder-slate-600
                              focus:outline-none focus:border-indigo-500 dark:focus:border-indigo-400 transition-colors" />
                <p *ngIf="form.get('nuevaPassword')?.invalid && form.get('nuevaPassword')?.touched"
                   class="text-xs text-red-600 dark:text-red-400 mt-1">Mínimo 8 caracteres</p>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Confirmar contraseña</label>
                <input formControlName="confirmar" type="password" placeholder="Repetí la contraseña"
                       class="w-full border border-gray-300 dark:border-indigo-900/40 rounded-lg px-3 py-2.5 text-sm
                              bg-white dark:bg-[#0a1628] text-gray-900 dark:text-slate-100
                              placeholder-gray-400 dark:placeholder-slate-600
                              focus:outline-none focus:border-indigo-500 dark:focus:border-indigo-400 transition-colors" />
                <p *ngIf="form.errors?.['noCoinciden'] && form.get('confirmar')?.touched"
                   class="text-xs text-red-600 dark:text-red-400 mt-1">Las contraseñas no coinciden</p>
              </div>

              <button type="submit" mat-flat-button color="primary" class="w-full" [disabled]="guardando()">
                {{ guardando() ? 'Guardando...' : 'Guardar nueva contraseña' }}
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class CambiarPasswordComponent {
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);

  readonly guardando = signal(false);
  readonly error     = signal('');

  form = this.fb.group({
    nuevaPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmar:     ['', Validators.required],
  }, { validators: passwordsIguales });

  guardar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.error.set('');
    this.guardando.set(true);

    this.auth.cambiarPassword(this.form.value.nuevaPassword!).subscribe({
      next: () => {
        this.guardando.set(false);
        this.router.navigate(['/cliente/dashboard']);
      },
      error: err => {
        this.error.set(err?.message ?? 'Error al cambiar la contraseña.');
        this.guardando.set(false);
      },
    });
  }
}
