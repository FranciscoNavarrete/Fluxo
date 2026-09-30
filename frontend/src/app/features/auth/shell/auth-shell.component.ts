import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-auth-shell',
  standalone: true,
  imports: [RouterOutlet, MatIconModule, MatTooltipModule],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-blue-50
                dark:from-gray-950 dark:via-gray-900 dark:to-gray-950
                flex items-center justify-center p-4 relative">

      <!-- Theme toggle -->
      <button
        (click)="theme.toggle()"
        [matTooltip]="theme.isDark() ? 'Modo claro' : 'Modo oscuro'"
        class="absolute top-4 right-4 w-9 h-9 flex items-center justify-center
               text-gray-400 hover:text-gray-700 dark:hover:text-white
               hover:bg-gray-100 dark:hover:bg-white/10
               transition-colors rounded-lg"
      >
        <mat-icon style="font-size:20px;width:20px;height:20px;line-height:20px">
          {{ theme.isDark() ? 'light_mode' : 'dark_mode' }}
        </mat-icon>
      </button>

      <div class="w-full max-w-[420px]">

        <!-- Logo -->
        <div class="text-center mb-8">
          <div class="inline-flex items-center justify-center mb-4">
            <img src="assets/fluxo-icon.svg" alt="Fluxo" class="w-14 h-14 drop-shadow-lg" />
          </div>
          <h1 class="text-2xl font-bold text-gray-800 dark:text-slate-100">Fluxo</h1>
          <p class="text-sm text-gray-400 dark:text-slate-500 mt-1">Sistema de gestión de suscripciones</p>
        </div>

        <router-outlet />

      </div>
    </div>
  `,
})
export class AuthShellComponent {
  readonly theme = inject(ThemeService);
}
