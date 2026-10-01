import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { SuscripcionService } from '../../../core/services/suscripcion.service';
import {
  ModalConfirmacionComponent,
  ModalConfirmacionData,
  ModalConfirmacionResult,
} from '../../../shared/components/modal-confirmacion/modal-confirmacion.component';
import { DunningResponse } from '../../../shared/models';

@Component({
  selector: 'app-monitoreo',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
  ],
  template: `
    <div class="max-w-3xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-semibold text-gray-800 dark:text-gray-100">Monitoreo</h1>
        <button
          mat-flat-button
          color="primary"
          [disabled]="ejecutandoDunning()"
          (click)="abrirModalDunning()"
        >
          <mat-icon>sync</mat-icon>
          {{ ejecutandoDunning() ? 'Ejecutando...' : 'Ejecutar Dunning Ahora' }}
        </button>
      </div>

      <!-- Resultado de dunning -->
      <mat-card *ngIf="resultadoDunning()" class="mb-6 border-l-4 border-green-500">
        <mat-card-content class="p-4">
          <div class="flex items-start gap-3">
            <mat-icon class="text-green-500 mt-0.5">check_circle</mat-icon>
            <div>
              <p class="font-semibold text-gray-800 dark:text-gray-100">Dunning ejecutado correctamente</p>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ resultadoDunning()!.mensaje }}</p>
            </div>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Info del proceso -->
      <mat-card>
        <mat-card-header>
          <mat-card-title class="text-base">¿Qué hace el proceso de Dunning?</mat-card-title>
        </mat-card-header>
        <mat-card-content class="p-4 pt-2">
          <ul class="text-sm text-gray-600 dark:text-gray-300 space-y-2">
            <li class="flex items-start gap-2">
              <mat-icon class="text-indigo-400 text-[16px] mt-0.5 shrink-0">chevron_right</mat-icon>
              Detecta suscripciones con pagos vencidos o rechazados.
            </li>
            <li class="flex items-start gap-2">
              <mat-icon class="text-indigo-400 text-[16px] mt-0.5 shrink-0">chevron_right</mat-icon>
              Escala automáticamente: email → WhatsApp → suspensión → cancelación.
            </li>
            <li class="flex items-start gap-2">
              <mat-icon class="text-indigo-400 text-[16px] mt-0.5 shrink-0">chevron_right</mat-icon>
              Se ejecuta automáticamente cada 6 horas. Este botón fuerza la ejecución manual.
            </li>
          </ul>

          <div class="mt-4 overflow-hidden rounded-lg border border-gray-100 dark:border-gray-700">
            <table class="w-full text-sm">
              <thead class="bg-gray-50 dark:bg-gray-800">
                <tr>
                  <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Días vencido</th>
                  <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Acción</th>
                  <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Estado suscripción</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
                <tr *ngFor="let row of escalasDunning">
                  <td class="px-4 py-2 text-gray-700 dark:text-gray-300">{{ row.dias }}</td>
                  <td class="px-4 py-2 text-gray-700 dark:text-gray-300">{{ row.accion }}</td>
                  <td class="px-4 py-2">
                    <span class="text-xs px-2 py-0.5 rounded-full" [ngClass]="row.badgeClass">
                      {{ row.estado }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
})
export class MonitoreoComponent {
  private readonly suscSvc  = inject(SuscripcionService);
  private readonly dialog   = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly ejecutandoDunning = signal(false);
  readonly resultadoDunning  = signal<DunningResponse | null>(null);

  readonly escalasDunning = [
    { dias: '1 – 3',  accion: 'Email',                       estado: 'Sin cambio',  badgeClass: 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300' },
    { dias: '3 – 7',  accion: 'Email + WhatsApp',            estado: 'Sin cambio',  badgeClass: 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300' },
    { dias: '7 – 9',  accion: 'Email + WhatsApp + aviso',    estado: 'Sin cambio',  badgeClass: 'bg-yellow-100 dark:bg-yellow-900/40 text-yellow-700 dark:text-yellow-400' },
    { dias: '9 – 15', accion: 'Suspender en MP',             estado: 'suspended',   badgeClass: 'bg-orange-100 dark:bg-orange-900/40 text-orange-700 dark:text-orange-400' },
    { dias: '15+',    accion: 'Cancelar en MP',              estado: 'cancelled',   badgeClass: 'bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-400' },
  ];

  abrirModalDunning(): void {
    const data: ModalConfirmacionData = {
      titulo:         'Ejecutar Dunning Manual',
      mensaje:        'Se procesarán todas las suscripciones con pagos vencidos según la escala de escalación. Puede tomar varios minutos.',
      labelConfirmar: 'Ejecutar ahora',
      tipo:           'warning',
    };

    this.dialog
      .open<ModalConfirmacionComponent, ModalConfirmacionData, ModalConfirmacionResult>(
        ModalConfirmacionComponent, { data, width: '460px' },
      )
      .afterClosed()
      .subscribe(res => { if (res?.confirmado) this.ejecutarDunning(); });
  }

  private ejecutarDunning(): void {
    this.ejecutandoDunning.set(true);
    this.resultadoDunning.set(null);
    this.suscSvc.ejecutarDunning().subscribe({
      next: res => {
        this.resultadoDunning.set(res);
        this.ejecutandoDunning.set(false);
      },
      error: err => {
        this.snackBar.open(err.message, 'OK', { duration: 5000, panelClass: 'snack-error' });
        this.ejecutandoDunning.set(false);
      },
    });
  }
}
