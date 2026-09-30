import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ResultadoPagoParams } from '../../shared/models';

@Component({
  selector: 'app-pagos-unicos-resultado',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule, MatIconModule],
  template: `
    <div class="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <mat-card class="max-w-md w-full">
        <mat-card-content class="p-10 text-center">

          <ng-container *ngIf="params()?.status === 'approved'">
            <mat-icon class="text-green-500 mb-4" style="font-size:64px; width:64px; height:64px">check_circle</mat-icon>
            <h1 class="text-2xl font-semibold text-gray-800 mb-2">¡Pago realizado!</h1>
            <p class="text-gray-500 text-sm mb-6">
              Tu pago único fue procesado correctamente.<br>
              ID de pago: <strong>{{ params()?.payment_id }}</strong>
            </p>
          </ng-container>

          <ng-container *ngIf="params()?.status === 'pending'">
            <mat-icon class="text-yellow-500 mb-4" style="font-size:64px; width:64px; height:64px">hourglass_top</mat-icon>
            <h1 class="text-2xl font-semibold text-gray-800 mb-2">Pago en proceso</h1>
            <p class="text-gray-500 text-sm mb-6">
              Tu pago está siendo verificado. Te notificaremos cuando se confirme.
            </p>
          </ng-container>

          <ng-container *ngIf="params()?.status === 'failure' || (!params()?.status)">
            <mat-icon class="text-red-500 mb-4" style="font-size:64px; width:64px; height:64px">cancel</mat-icon>
            <h1 class="text-2xl font-semibold text-gray-800 mb-2">Pago no procesado</h1>
            <p class="text-gray-500 text-sm mb-6">
              Hubo un problema al procesar tu pago. Podés intentarlo de nuevo desde Pago Único.
            </p>
          </ng-container>

          <button mat-flat-button color="primary" routerLink="/cliente/dashboard">
            Ir a mi cuenta
          </button>

        </mat-card-content>
      </mat-card>
    </div>
  `,
})
export class PagosUnicosResultadoComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  readonly params = signal<ResultadoPagoParams | null>(null);

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParams;
    this.params.set({
      status:             qp['status'] ?? '',
      payment_id:         qp['payment_id'] ?? '',
      external_reference: qp['external_reference'] ?? '',
      merchant_order_id:  qp['merchant_order_id'],
    });
  }
}
