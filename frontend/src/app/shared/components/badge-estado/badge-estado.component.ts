import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EstadoPago, EstadoSuscripcion } from '../../models';

type EstadoUnion = EstadoSuscripcion | EstadoPago;

interface BadgeConfig {
  label: string;
  classes: string;
}

const CONFIG: Record<string, BadgeConfig> = {
  authorized: { label: 'Activa',      classes: 'bg-green-100 text-green-800' },
  approved:   { label: 'Aprobado',    classes: 'bg-green-100 text-green-800' },
  aprobado:   { label: 'Aprobado',    classes: 'bg-green-100 text-green-800' },
  pending:    { label: 'Pendiente',   classes: 'bg-yellow-100 text-yellow-800' },
  pendiente:  { label: 'Pendiente',   classes: 'bg-yellow-100 text-yellow-800' },
  paused:     { label: 'Pausada',     classes: 'bg-orange-100 text-orange-700' },
  suspended:  { label: 'Suspendida',  classes: 'bg-orange-100 text-orange-700' },
  cancelled:  { label: 'Cancelada',   classes: 'bg-red-100 text-red-700' },
  rechazado:  { label: 'Rechazado',   classes: 'bg-red-100 text-red-700' },
  failure:    { label: 'Fallido',     classes: 'bg-red-100 text-red-700' },
};

const FALLBACK: BadgeConfig = { label: 'Desconocido', classes: 'bg-gray-100 text-gray-600' };

@Component({
  selector: 'app-badge-estado',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span
      [ngClass]="config.classes"
      class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium capitalize"
    >
      {{ config.label }}
    </span>
  `,
})
export class BadgeEstadoComponent {
  @Input({ required: true }) estado!: EstadoUnion | string;

  get config(): BadgeConfig {
    return CONFIG[this.estado] ?? FALLBACK;
  }
}
