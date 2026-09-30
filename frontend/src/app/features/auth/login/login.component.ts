import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatProgressSpinnerModule,
  ],
  template: `
    <div class="bg-white dark:bg-[#0f1e3d] rounded-2xl shadow-xl border border-gray-100 dark:border-indigo-900/40 overflow-hidden">

      <div class="px-8 pt-7 pb-5 border-b border-gray-100 dark:border-indigo-900/30">
        <h2 class="text-xl font-semibold text-gray-800 dark:text-slate-100">Iniciar sesión</h2>
        <p class="text-sm text-gray-400 dark:text-slate-500 mt-0.5">Ingresá tus credenciales para continuar</p>
      </div>

      <div class="px-8 py-7">
        <div *ngIf="errorMsg()" class="mb-4 px-4 py-3 rounded-xl bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 flex items-start gap-2">
          <mat-icon class="text-red-500 text-[18px] mt-0.5 shrink-0">error_outline</mat-icon>
          <p class="text-sm text-red-700 dark:text-red-400">{{ errorMsg() }}</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-3">
          <mat-form-field appearance="outline" class="w-full" subscriptSizing="dynamic">
            <mat-label>Email</mat-label>
            <mat-icon matPrefix class="mr-2 text-gray-400 text-[18px]">mail_outline</mat-icon>
            <input matInput type="email" formControlName="email" placeholder="usuario@ejemplo.com" />
            <mat-error *ngIf="form.get('email')?.hasError('required')">Requerido</mat-error>
            <mat-error *ngIf="form.get('email')?.hasError('email')">Email inválido</mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full" subscriptSizing="dynamic">
            <mat-label>Contraseña</mat-label>
            <mat-icon matPrefix class="mr-2 text-gray-400 text-[18px]">lock_outline</mat-icon>
            <input matInput [type]="mostrarPassword() ? 'text' : 'password'" formControlName="password" />
            <button mat-icon-button matSuffix type="button" (click)="mostrarPassword.set(!mostrarPassword())">
              <mat-icon class="text-[18px]">{{ mostrarPassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
            </button>
          </mat-form-field>

          <button mat-flat-button color="primary" type="submit" class="w-full h-11 mt-2"
                  [disabled]="cargando() || form.invalid">
            <mat-spinner *ngIf="cargando()" diameter="20" class="inline-block mr-2"></mat-spinner>
            {{ cargando() ? 'Ingresando...' : 'Ingresar' }}
          </button>
        </form>

        <div class="flex items-center justify-between mt-6 text-sm text-gray-400 dark:text-slate-500">
          <a routerLink="/auth/recuperar" class="text-indigo-600 dark:text-indigo-400 hover:underline">
            ¿Olvidaste tu contraseña?
          </a>
          <span>
            ¿No tenés cuenta?
            <a routerLink="/auth/registro" class="text-indigo-600 dark:text-indigo-400 hover:underline font-medium">
              Registrate
            </a>
          </span>
        </div>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);

  readonly cargando        = signal(false);
  readonly mostrarPassword = signal(false);
  readonly errorMsg        = signal('');

  form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.errorMsg.set('');
    this.cargando.set(true);
    const { email, password } = this.form.value;
    this.auth.login({ email: email!, password: password! }).subscribe({
      next:  () => { this.cargando.set(false); this.redirigir(); },
      error: err => { this.errorMsg.set(err.message ?? 'Credenciales incorrectas'); this.cargando.set(false); },
    });
  }

  private redirigir(): void {
    const rol = this.auth.getRole();
    if (rol === 'ADMINISTRADOR' || rol === 'SISTEMA') {
      this.router.navigate(['/admin']);
    } else {
      this.router.navigate(['/cliente']);
    }
  }
}
