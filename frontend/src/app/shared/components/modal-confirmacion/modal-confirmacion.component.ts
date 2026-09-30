import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ModalConfirmacionData {
  titulo: string;
  mensaje: string;
  labelConfirmar?: string;
  labelCancelar?: string;
  tipo?: 'danger' | 'warning' | 'info';
  campoTexto?: {
    label: string;
    placeholder?: string;
    requerido?: boolean;
  };
}

export interface ModalConfirmacionResult {
  confirmado: boolean;
  texto?: string;
}

@Component({
  selector: 'app-modal-confirmacion',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  template: `
    <div class="p-1">
      <h2 mat-dialog-title class="text-lg font-semibold">{{ data.titulo }}</h2>

      <mat-dialog-content class="mt-2">
        <p class="text-gray-600 text-sm">{{ data.mensaje }}</p>

        <mat-form-field *ngIf="data.campoTexto" class="w-full mt-4" appearance="outline">
          <mat-label>{{ data.campoTexto.label }}</mat-label>
          <textarea
            matInput
            rows="3"
            [placeholder]="data.campoTexto.placeholder ?? ''"
            [formControl]="textControl"
          ></textarea>
          <mat-error *ngIf="textControl.hasError('required')">
            Este campo es requerido
          </mat-error>
        </mat-form-field>
      </mat-dialog-content>

      <mat-dialog-actions align="end" class="gap-2 pt-2">
        <button mat-stroked-button (click)="cancelar()">
          {{ data.labelCancelar ?? 'Cancelar' }}
        </button>
        <button
          mat-flat-button
          [color]="colorBoton"
          (click)="confirmar()"
          [disabled]="data.campoTexto?.requerido && textControl.invalid"
        >
          {{ data.labelConfirmar ?? 'Confirmar' }}
        </button>
      </mat-dialog-actions>
    </div>
  `,
})
export class ModalConfirmacionComponent {
  textControl: FormControl;

  constructor(
    public dialogRef: MatDialogRef<ModalConfirmacionComponent, ModalConfirmacionResult>,
    @Inject(MAT_DIALOG_DATA) public data: ModalConfirmacionData,
  ) {
    this.textControl = new FormControl(
      '',
      data.campoTexto?.requerido ? [Validators.required, Validators.minLength(5)] : [],
    );
  }

  get colorBoton(): string {
    return this.data.tipo === 'danger' ? 'warn' : 'primary';
  }

  confirmar(): void {
    if (this.data.campoTexto?.requerido && this.textControl.invalid) {
      this.textControl.markAsTouched();
      return;
    }
    this.dialogRef.close({ confirmado: true, texto: this.textControl.value ?? undefined });
  }

  cancelar(): void {
    this.dialogRef.close({ confirmado: false });
  }
}
