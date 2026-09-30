import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/services/auth.service';

function passwordsIguales(ctrl: AbstractControl): ValidationErrors | null {
  const p = ctrl.get('nuevaPassword')?.value;
  const c = ctrl.get('confirmar')?.value;
  return p && c && p !== c ? { noCoinciden: true } : null;
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="w-full max-w-sm mx-auto">
      <div class="bg-white dark:bg-[#0f1e3d] rounded-2xl shadow-xl border border-gray-100 dark:border-indigo-900/40 overflow-hidden">

        <!-- Header -->
        <div class="px-8 pt-8 pb-6 border-b border-gray-100 dark:border-indigo-900/30">
          <div class="w-12 h-12 rounded-xl bg-indigo-100 dark:bg-indigo-900/40 flex items-center justify-center mb-4">
            <mat-icon class="text-indigo-600 dark:text-indigo-400">lock</mat-icon>
          </div>
          <h1 class="text-xl font-bold text-gray-900 dark:text-slate-100">Nueva contraseña</h1>
          <p class="text-sm text-gray-500 dark:text-slate-500 mt-1">Elegí una contraseña de al menos 8 caracteres.</p>
        </div>

        <div class="px-8 py-6">

          <!-- Token inválido -->
          <div *ngIf="tokenInvalido()" class="flex flex-col items-center text-center gap-4 py-4">
            <mat-icon class="text-red-400" style="font-size:40px;width:40px;height:40px;line-height:40px">link_off</mat-icon>
            <div>
              <p class="font-semibold text-gray-800 dark:text-slate-100">Enlace inválido o expirado</p>
              <p class="text-sm text-gray-500 dark:text-slate-400 mt-1">Solicitá uno nuevo desde la pantalla de recuperación.</p>
            </div>
            <a routerLink="/auth/recuperar" mat-flat-button color="primary" class="w-full">Solicitar nuevo enlace</a>
          </div>

          <!-- Éxito -->
          <div *ngIf="exito()" class="flex flex-col items-center text-center gap-4 py-4">
            <div class="w-14 h-14 rounded-full bg-green-100 dark:bg-green-900/30 flex items-center justify-center">
              <mat-icon class="text-green-500" style="font-size:28px;width:28px;height:28px;line-height:28px">check_circle</mat-icon>
            </div>
            <div>
              <p class="font-semibold text-gray-800 dark:text-slate-100">Contraseña actualizada</p>
              <p class="text-sm text-gray-500 dark:text-slate-400 mt-1">Ya podés ingresar con tu nueva contraseña.</p>
            </div>
            <a routerLink="/auth/login" mat-flat-button color="primary" class="w-full">Ir al login</a>
          </div>

          <!-- Formulario -->
          <ng-container *ngIf="!tokenInvalido() && !exito()">
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
                {{ guardando() ? 'Guardando...' : 'Cambiar contraseña' }}
              </button>
            </form>
          </ng-container>
        </div>
      </div>
    </div>
  `,
})
export class ResetPasswordComponent implements OnInit {
  private readonly auth  = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb    = inject(FormBuilder);

  readonly guardando    = signal(false);
  readonly exito        = signal(false);
  readonly error        = signal('');
  readonly tokenInvalido = signal(false);

  private token = '';

  form = this.fb.group({
    nuevaPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmar:     ['', Validators.required],
  }, { validators: passwordsIguales });

  ngOnInit(): void {
    const t = this.route.snapshot.queryParamMap.get('token');
    if (!t) { this.tokenInvalido.set(true); return; }
    this.token = t;
  }

  guardar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.error.set('');
    this.guardando.set(true);

    this.auth.resetPassword(this.token, this.form.value.nuevaPassword!).subscribe({
      next: () => { this.exito.set(true); this.guardando.set(false); },
      error: err => {
        this.error.set(err?.message ?? 'El enlace es inválido o ya expiró.');
        this.guardando.set(false);
      },
    });
  }
}
