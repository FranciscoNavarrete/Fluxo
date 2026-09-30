import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-recuperar',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="w-full max-w-sm mx-auto">
      <div class="bg-white dark:bg-[#0f1e3d] rounded-2xl shadow-xl border border-gray-100 dark:border-indigo-900/40 overflow-hidden">

        <!-- Header -->
        <div class="px-8 pt-8 pb-6 border-b border-gray-100 dark:border-indigo-900/30">
          <div class="w-12 h-12 rounded-xl bg-indigo-100 dark:bg-indigo-900/40 flex items-center justify-center mb-4">
            <mat-icon class="text-indigo-600 dark:text-indigo-400">lock_reset</mat-icon>
          </div>
          <h1 class="text-xl font-bold text-gray-900 dark:text-slate-100">Recuperar contraseña</h1>
          <p class="text-sm text-gray-500 dark:text-slate-500 mt-1">
            Ingresá tu email y te enviaremos las instrucciones.
          </p>
        </div>

        <div class="px-8 py-6">

          <!-- Éxito -->
          <ng-container *ngIf="enviado(); else formBlock">
            <div class="flex flex-col items-center text-center gap-4 py-4">
              <div class="w-14 h-14 rounded-full bg-green-100 dark:bg-green-900/30 flex items-center justify-center">
                <mat-icon class="text-green-500" style="font-size:28px;width:28px;height:28px;line-height:28px">check_circle</mat-icon>
              </div>
              <div>
                <p class="font-semibold text-gray-800 dark:text-slate-100">¡Listo!</p>
                <p class="text-sm text-gray-500 dark:text-slate-400 mt-1">
                  Si el email existe en el sistema, recibirás las instrucciones para restablecer tu contraseña.
                </p>
              </div>
              <!-- En dev: mostramos el token directamente -->
              <div *ngIf="tokenDev()" class="w-full rounded-lg bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700/40 p-4 text-left">
                <p class="text-xs font-semibold text-amber-700 dark:text-amber-400 mb-2">
                  <mat-icon style="font-size:14px;vertical-align:middle">warning</mat-icon>
                  Token de desarrollo (no visible en producción)
                </p>
                <a [routerLink]="['/auth/reset-password']" [queryParams]="{ token: tokenDev() }"
                   class="text-xs font-mono break-all text-indigo-600 dark:text-indigo-400 underline">
                  Ir a restablecer contraseña →
                </a>
              </div>
              <a routerLink="/auth/login" mat-stroked-button class="w-full mt-2">Volver al login</a>
            </div>
          </ng-container>

          <ng-template #formBlock>
            <div *ngIf="error()" class="mb-4 px-4 py-3 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/40 flex items-start gap-2">
              <mat-icon class="text-red-500 shrink-0 text-[18px] mt-0.5">error_outline</mat-icon>
              <p class="text-sm text-red-700 dark:text-red-400">{{ error() }}</p>
            </div>

            <form [formGroup]="form" (ngSubmit)="enviar()" class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Email</label>
                <input formControlName="email" type="email" placeholder="tu@email.com"
                       class="w-full border border-gray-300 dark:border-indigo-900/40 rounded-lg px-3 py-2.5 text-sm
                              bg-white dark:bg-[#0a1628] text-gray-900 dark:text-slate-100
                              placeholder-gray-400 dark:placeholder-slate-600
                              focus:outline-none focus:border-indigo-500 dark:focus:border-indigo-400 transition-colors" />
                <p *ngIf="form.get('email')?.invalid && form.get('email')?.touched"
                   class="text-xs text-red-600 dark:text-red-400 mt-1">Email inválido</p>
              </div>

              <button type="submit" mat-flat-button color="primary" class="w-full" [disabled]="cargando()">
                {{ cargando() ? 'Enviando...' : 'Enviar instrucciones' }}
              </button>
            </form>

            <p class="text-center text-sm text-gray-500 dark:text-slate-500 mt-5">
              <a routerLink="/auth/login" class="text-indigo-600 dark:text-indigo-400 hover:underline font-medium">
                ← Volver al login
              </a>
            </p>
          </ng-template>
        </div>
      </div>
    </div>
  `,
})
export class RecuperarComponent {
  private readonly auth = inject(AuthService);
  private readonly fb   = inject(FormBuilder);

  readonly cargando = signal(false);
  readonly enviado  = signal(false);
  readonly error    = signal('');
  readonly tokenDev = signal('');

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  enviar(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.error.set('');
    this.cargando.set(true);

    this.auth.solicitarReset(this.form.value.email!).subscribe({
      next: res => {
        this.enviado.set(true);
        this.cargando.set(false);
        if (res?.token) this.tokenDev.set(res.token);
      },
      error: () => {
        this.error.set('Ocurrió un error. Intentá de nuevo más tarde.');
        this.cargando.set(false);
      },
    });
  }
}
