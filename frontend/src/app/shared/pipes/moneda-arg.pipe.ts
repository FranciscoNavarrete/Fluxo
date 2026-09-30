import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'monedaArg', standalone: true })
export class MonedaArgPipe implements PipeTransform {
  private formatter = new Intl.NumberFormat('es-AR', {
    style:                 'currency',
    currency:              'ARS',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

  transform(value: number | null | undefined, moneda: string = 'ARS'): string {
    if (value === null || value === undefined) return '-';
    if (moneda === 'USD') {
      return new Intl.NumberFormat('es-AR', {
        style: 'currency', currency: 'USD',
        minimumFractionDigits: 2,
      }).format(value);
    }
    return this.formatter.format(value);
  }
}
