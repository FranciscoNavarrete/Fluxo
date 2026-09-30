import { Pipe, PipeTransform } from '@angular/core';
import { EstadoSuscripcion } from '../models';

const ETIQUETAS: Record<EstadoSuscripcion, string> = {
  authorized: 'Activa',
  pending:    'Pendiente',
  paused:     'Pausada',
  suspended:  'Suspendida',
  cancelled:  'Cancelada',
};

@Pipe({ name: 'estadoSuscripcion', standalone: true })
export class EstadoSuscripcionPipe implements PipeTransform {
  transform(value: EstadoSuscripcion | string): string {
    return ETIQUETAS[value as EstadoSuscripcion] ?? value;
  }
}
