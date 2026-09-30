import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';

interface NavItem {
  label: string;
  icon: string;
  path: string;
}

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    RouterLink,
    RouterLinkActive,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
  ],
  styles: [`:host { display: flex; height: 100vh; overflow: hidden; }`],
  template: `
    <!-- Sidebar -->
    <aside class="w-60 flex-shrink-0 bg-gray-950 flex flex-col h-full">

      <!-- Logo -->
      <div class="flex items-center gap-3 px-5 py-5 border-b border-white/10">
        <img src="assets/fluxo-icon.svg" alt="Fluxo" class="w-8 h-8 flex-shrink-0" />
        <div>
          <p class="text-white font-semibold text-sm leading-tight">Fluxo</p>
          <span class="text-[10px] text-indigo-400 font-medium uppercase tracking-wider">Admin</span>
        </div>
      </div>

      <!-- Nav -->
      <nav class="flex-1 py-4 overflow-y-auto">
        <p class="text-[10px] text-gray-500 font-semibold uppercase tracking-widest px-5 mb-2">Gestión</p>
        <a
          *ngFor="let item of navItems"
          [routerLink]="item.path"
          routerLinkActive="bg-indigo-600/20 text-indigo-300 border-l-2 border-indigo-500"
          [routerLinkActiveOptions]="{ exact: false }"
          class="flex items-center gap-3 px-5 py-2.5 mx-2 text-sm text-gray-400 hover:bg-white/5 hover:text-white transition-all rounded-lg mb-0.5 border-l-2 border-transparent"
        >
          <mat-icon style="font-size:18px;width:18px;height:18px;line-height:18px;flex-shrink:0">{{ item.icon }}</mat-icon>
          {{ item.label }}
        </a>
      </nav>

      <!-- User footer -->
      <div class="border-t border-white/10 p-4">
        <div class="flex items-center gap-3 mb-3">
          <div class="w-8 h-8 rounded-full bg-indigo-600 flex items-center justify-center flex-shrink-0">
            <span class="text-white text-xs font-bold">
              {{ (auth.usuario()?.email ?? 'A')[0].toUpperCase() }}
            </span>
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-xs font-medium text-gray-200 truncate">{{ auth.usuario()?.email }}</p>
            <p class="text-[10px] text-gray-500">{{ auth.usuario()?.role }}</p>
          </div>
          <button
            (click)="theme.toggle()"
            [matTooltip]="theme.isDark() ? 'Modo claro' : 'Modo oscuro'"
            class="w-7 h-7 flex items-center justify-center text-gray-400 hover:text-white
                   hover:bg-white/10 transition-colors rounded-lg flex-shrink-0"
          >
            <mat-icon style="font-size:16px;width:16px;height:16px;line-height:16px">
              {{ theme.isDark() ? 'light_mode' : 'dark_mode' }}
            </mat-icon>
          </button>
        </div>
        <button
          (click)="auth.logout()"
          class="w-full flex items-center justify-center gap-2 text-xs text-gray-400 hover:text-white
                 hover:bg-white/10 transition-colors rounded-lg py-2 px-3"
        >
          <mat-icon style="font-size:16px;width:16px;height:16px;line-height:16px">logout</mat-icon>
          Cerrar sesión
        </button>
      </div>
    </aside>

    <!-- Content -->
    <div class="flex-1 flex flex-col overflow-hidden bg-gray-50 dark:bg-[#080f1e]">
      <div class="flex-1 overflow-y-auto">
        <main class="max-w-7xl mx-auto px-6 py-6">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class AdminShellComponent {
  readonly auth  = inject(AuthService);
  readonly theme = inject(ThemeService);

  readonly navItems: NavItem[] = [
    { label: 'Suscripciones', icon: 'subscriptions', path: 'suscripciones'  },
    { label: 'Nuevo cliente', icon: 'person_add',    path: 'clientes/nuevo' },
    { label: 'Planes',        icon: 'inventory_2',   path: 'planes'         },
    { label: 'Monitoreo',     icon: 'monitor_heart', path: 'monitoreo'      },
  ];
}
