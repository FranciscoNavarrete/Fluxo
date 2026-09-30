import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { RegistroRequest } from '../../../shared/models';

function passwordsCoinciden(control: AbstractControl): ValidationErrors | null {
  const password  = control.get('password')?.value;
  const confirmar = control.get('confirmarPassword')?.value;
  return password && confirmar && password !== confirmar ? { noCoinciden: true } : null;
}

@Component({
  selector: 'app-registro',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <mat-card class="shadow-xl border-0">
      <mat-card-content class="p-8">
        <h2 class="text-xl font-semibold text-gray-800 mb-1">Crear cuenta</h2>
        <p class="text-sm text-gray-400 mb-6">Completá tus datos para registrarte</p>

        <!-- Error global -->
        <div *ngIf="errorMsg()" class="mb-5 px-4 py-3 rounded-lg bg-red-50 border border-red-200 flex items-start gap-2">
          <mat-icon class="text-red-500 text-[18px] mt-0.5 shrink-0">error_outline</mat-icon>
          <p class="text-sm text-red-700">{{ errorMsg() }}</p>
        </div>

        <!-- Éxito -->
        <div *ngIf="exito()" class="mb-5 px-4 py-3 rounded-lg bg-green-50 border border-green-200 flex items-start gap-2">
          <mat-icon class="text-green-500 text-[18px] mt-0.5 shrink-0">check_circle</mat-icon>
          <p class="text-sm text-green-700">
            Cuenta creada correctamente. Redirigiendo al inicio de sesión...
          </p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-3">

          <div class="grid grid-cols-2 gap-3">
            <mat-form-field appearance="outline">
              <mat-label>Nombre</mat-label>
              <input matInput formControlName="nombre" />
              <mat-error *ngIf="form.get('nombre')?.hasError('required')">Requerido</mat-error>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Apellido</mat-label>
              <input matInput formControlName="apellido" />
              <mat-error *ngIf="form.get('apellido')?.hasError('required')">Requerido</mat-error>
            </mat-form-field>
          </div>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
            <mat-icon matPrefix class="mr-2 text-gray-400">mail_outline</mat-icon>
            <mat-error *ngIf="form.get('email')?.hasError('required')">El email es requerido</mat-error>
            <mat-error *ngIf="form.get('email')?.hasError('email')">Email inválido</mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>DNI (opcional)</mat-label>
            <input matInput formControlName="dni" placeholder="12345678" />
            <mat-icon matPrefix class="mr-2 text-gray-400">badge</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Teléfono (opcional)</mat-label>
            <input matInput formControlName="telefono" placeholder="+54 11 1234-5678" />
            <mat-icon matPrefix class="mr-2 text-gray-400">phone</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Contraseña</mat-label>
            <input matInput [type]="mostrarPassword() ? 'text' : 'password'"
                   formControlName="password" autocomplete="new-password" />
            <mat-icon matPrefix class="mr-2 text-gray-400">lock_outline</mat-icon>
            <button mat-icon-button matSuffix type="button"
                    (click)="mostrarPassword.set(!mostrarPassword())">
              <mat-icon>{{ mostrarPassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
            </button>
            <mat-hint>Mínimo 8 caracteres</mat-hint>
            <mat-error *ngIf="form.get('password')?.hasError('required')">Requerida</mat-error>
            <mat-error *ngIf="form.get('password')?.hasError('minlength')">Mínimo 8 caracteres</mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Confirmar contraseña</mat-label>
            <input matInput [type]="mostrarPassword() ? 'text' : 'password'"
                   formControlName="confirmarPassword" autocomplete="new-password" />
            <mat-icon matPrefix class="mr-2 text-gray-400">lock_outline</mat-icon>
            <mat-error *ngIf="form.get('confirmarPassword')?.hasError('required')">Requerida</mat-error>
            <mat-error *ngIf="form.hasError('noCoinciden')">Las contraseñas no coinciden</mat-error>
          </mat-form-field>

          <button
            mat-flat-button
            color="primary"
            type="submit"
            class="w-full h-11 text-base mt-2"
            [disabled]="cargando() || form.invalid || exito()"
          >
            <mat-spinner *ngIf="cargando()" diameter="20" class="inline-block mr-2" />
            {{ cargando() ? 'Registrando...' : 'Crear cuenta' }}
          </button>
        </form>

        <p class="text-center text-sm text-gray-400 mt-6">
          ¿Ya tenés cuenta?
          <a routerLink="/auth/login" class="text-indigo-600 hover:underline font-medium">
            Iniciá sesión
          </a>
        </p>
      </mat-card-content>
    </mat-card>
  `,
})
export class RegistroComponent {
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);

  readonly cargando       = signal(false);
  readonly mostrarPassword = signal(false);
  readonly errorMsg       = signal('');
  readonly exito          = signal(false);

  form = this.fb.group(
    {
      nombre:            ['', Validators.required],
      apellido:          ['', Validators.required],
      email:             ['', [Validators.required, Validators.email]],
      dni:               [''],
      telefono:          [''],
      password:          ['', [Validators.required, Validators.minLength(8)]],
      confirmarPassword: ['', Validators.required],
    },
    { validators: passwordsCoinciden },
  );

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMsg.set('');
    this.cargando.set(true);

    const val = this.form.value;
    const req: RegistroRequest = {
      nombre:   val.nombre!,
      apellido: val.apellido!,
      email:    val.email!,
      password: val.password!,
      dni:      val.dni || undefined,
      telefono: val.telefono || undefined,
    };

    this.auth.registro(req).subscribe({
      next: () => {
        this.cargando.set(false);
        this.router.navigate(['/cliente']);
      },
      error: err => {
        this.errorMsg.set(err.message ?? 'No se pudo crear la cuenta. Intentá de nuevo.');
        this.cargando.set(false);
      },
    });
  }
}
