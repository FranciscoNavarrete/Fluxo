import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SuscripcionService } from '../../../core/services/suscripcion.service';
import { MonedaArgPipe } from '../../../shared/pipes/moneda-arg.pipe';
import { MpPlan, CrearSuscripcionResponse } from '../../../shared/models';

@Component({
  selector: 'app-crear-suscripcion-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MonedaArgPipe,
  ],
  styles: [`:host { display:block; width:480px; background:#fff; border-radius:12px; overflow:hidden; }`],
  template: `
    <!-- Header -->
    <div style="display:flex;align-items:center;justify-content:space-between;padding:20px 24px;border-bottom:1px solid #e5e7eb">
      <span style="font-size:17px;font-weight:600;color:#111827">Nueva Suscripción</span>
      <button mat-icon-button (click)="cerrar()"><mat-icon>close</mat-icon></button>
    </div>

    <!-- Body -->
    <div style="padding:20px 24px">

      <!-- Éxito -->
      <div *ngIf="resultado" style="background:#f0fdf4;border:1px solid #86efac;border-radius:10px;padding:16px;margin-bottom:16px">
        <p style="font-weight:600;color:#16a34a;margin:0 0 8px">✓ Suscripción creada · #{{ resultado.mpSuscripcionId }}</p>
        <p style="font-size:12px;color:#6b7280;margin:0 0 6px">Link de pago Mercado Pago:</p>
        <div style="display:flex;gap:6px;align-items:center">
          <input readonly [value]="resultado.initPoint"
                 style="flex:1;font-size:11px;font-family:monospace;border:1px solid #d1d5db;border-radius:6px;padding:6px 8px;min-width:0" />
          <button mat-icon-button (click)="copiarLink()" title="Copiar">
            <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px">content_copy</mat-icon>
          </button>
          <a [href]="resultado.initPoint" target="_blank" mat-icon-button title="Abrir en MP">
            <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px">open_in_new</mat-icon>
          </a>
        </div>
      </div>

      <!-- Error -->
      <div *ngIf="errorMsg" style="background:#fef2f2;border:1px solid #fca5a5;border-radius:8px;padding:10px 14px;margin-bottom:14px">
        <p style="font-size:13px;color:#dc2626;margin:0">{{ errorMsg }}</p>
      </div>

      <!-- Form -->
      <form *ngIf="!resultado" [formGroup]="form" (ngSubmit)="crear()">

        <!-- Plan -->
        <div style="margin-bottom:14px">
          <label style="display:block;font-size:13px;font-weight:500;color:#374151;margin-bottom:4px">Plan *</label>
          <select formControlName="mpPlanId"
                  style="width:100%;border:1px solid #d1d5db;border-radius:8px;padding:9px 12px;font-size:14px;color:#111827;background:#fff;outline:none">
            <option value="" disabled selected>Seleccioná un plan</option>
            <option *ngFor="let p of planes" [value]="p.mpPlanId">
              {{ p.nombre }} — {{ p.monto | monedaArg:p.moneda }}
            </option>
          </select>
          <p *ngIf="form.get('mpPlanId')?.invalid && form.get('mpPlanId')?.touched"
             style="font-size:11px;color:#dc2626;margin:3px 0 0">Seleccioná un plan</p>
        </div>

        <!-- ID Cliente -->
        <div style="margin-bottom:14px">
          <label style="display:block;font-size:13px;font-weight:500;color:#374151;margin-bottom:4px">ID Cliente *</label>
          <input type="number" formControlName="clienteId" placeholder="Ej: 42"
                 style="width:100%;border:1px solid #d1d5db;border-radius:8px;padding:9px 12px;font-size:14px;color:#111827;box-sizing:border-box;outline:none" />
          <p *ngIf="form.get('clienteId')?.invalid && form.get('clienteId')?.touched"
             style="font-size:11px;color:#dc2626;margin:3px 0 0">Requerido</p>
        </div>

        <!-- Email -->
        <div style="margin-bottom:6px">
          <label style="display:block;font-size:13px;font-weight:500;color:#374151;margin-bottom:4px">Email del cliente *</label>
          <input type="email" formControlName="payerEmail" placeholder="cliente@email.com"
                 style="width:100%;border:1px solid #d1d5db;border-radius:8px;padding:9px 12px;font-size:14px;color:#111827;box-sizing:border-box;outline:none" />
          <p *ngIf="form.get('payerEmail')?.invalid && form.get('payerEmail')?.touched"
             style="font-size:11px;color:#dc2626;margin:3px 0 0">Email válido requerido</p>
        </div>

      </form>
    </div>

    <!-- Footer -->
    <div style="display:flex;justify-content:flex-end;gap:8px;padding:12px 24px 20px;border-top:1px solid #e5e7eb">
      <ng-container *ngIf="!resultado">
        <button mat-stroked-button (click)="cerrar()">Cancelar</button>
        <button mat-flat-button color="primary" (click)="crear()" [disabled]="form.invalid || cargando">
          <mat-spinner *ngIf="cargando" diameter="16" style="display:inline-block;margin-right:6px"></mat-spinner>
          {{ cargando ? 'Creando...' : 'Crear suscripción' }}
        </button>
      </ng-container>
      <button mat-flat-button color="primary" *ngIf="resultado" (click)="cerrar()">Cerrar</button>
    </div>
  `,
})
export class CrearSuscripcionDialogComponent implements OnInit {
  private readonly suscSvc   = inject(SuscripcionService);
  private readonly dialogRef = inject(MatDialogRef<CrearSuscripcionDialogComponent>);
  private readonly fb        = inject(FormBuilder);

  planes:    MpPlan[]                       = [];
  cargando:  boolean                        = false;
  errorMsg:  string                         = '';
  resultado: CrearSuscripcionResponse | null = null;

  form = this.fb.group({
    mpPlanId:   [null as number | null, Validators.required],
    clienteId:  [null as number | null, Validators.required],
    payerEmail: ['', [Validators.required, Validators.email]],
  });

  ngOnInit(): void {
    this.suscSvc.getPlanesActivos().subscribe({
      next:  planes => { this.planes = planes.filter(p => p.activo); },
      error: ()     => {},
    });
  }

  crear(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.errorMsg = '';
    this.cargando = true;
    const v = this.form.value;
    this.suscSvc.crearConRedirect({
      clienteId:  v.clienteId!,
      mpPlanId:   v.mpPlanId!,
      payerEmail: v.payerEmail!,
    }).subscribe({
      next:  res => { this.resultado = res; this.cargando = false; },
      error: err => { this.errorMsg = err.message ?? 'Error al crear'; this.cargando = false; },
    });
  }

  copiarLink(): void {
    if (this.resultado?.initPoint) navigator.clipboard.writeText(this.resultado.initPoint);
  }

  cerrar(): void { this.dialogRef.close(!!this.resultado); }
}
