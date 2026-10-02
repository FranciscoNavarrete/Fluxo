import { signal } from '@angular/core';

const INTERVALO_MS = 30_000;
const DURACION_MAXIMA_MS = 15 * 60_000;

/**
 * Vuelve a consultar una lista cada 30 segundos, pero solo mientras haya algo pendiente a la vista,
 * la pestaña esté visible y no hayan pasado 15 minutos. Al volver a la pestaña refresca una vez.
 */
export class RefrescoAutomatico {
  readonly activo = signal(false);
  readonly expirado = signal(false);

  private timer: ReturnType<typeof setInterval> | null = null;
  private inicio = 0;

  private readonly alCambiarVisibilidad = () => {
    if (document.visibilityState !== 'visible') return;
    if (this.activo()) {
      this.refrescar();
    } else if (this.expirado()) {
      this.expirado.set(false);
      this.refrescar();
    }
  };

  constructor(private readonly refrescar: () => void) {
    document.addEventListener('visibilitychange', this.alCambiarVisibilidad);
  }

  /** Llamar después de cada carga de la lista, diciendo si quedó algo pendiente. */
  evaluar(hayPendientes: boolean): void {
    if (!hayPendientes) {
      this.detener();
      this.expirado.set(false);
      return;
    }
    if (!this.activo() && !this.expirado()) this.iniciar();
  }

  reanudar(): void {
    this.expirado.set(false);
  }

  destruir(): void {
    this.detener();
    document.removeEventListener('visibilitychange', this.alCambiarVisibilidad);
  }

  private iniciar(): void {
    this.inicio = Date.now();
    this.activo.set(true);
    this.timer = setInterval(() => this.tick(), INTERVALO_MS);
  }

  private tick(): void {
    if (document.hidden) return;
    if (Date.now() - this.inicio >= DURACION_MAXIMA_MS) {
      this.detener();
      this.expirado.set(true);
      return;
    }
    this.refrescar();
  }

  private detener(): void {
    if (this.timer) clearInterval(this.timer);
    this.timer = null;
    this.activo.set(false);
  }
}
