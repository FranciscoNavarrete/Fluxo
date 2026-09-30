import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  template: `
    <div *ngIf="visible" class="flex flex-col items-center justify-center gap-3 py-12">
      <mat-spinner [diameter]="diameter" />
      <p *ngIf="mensaje" class="text-sm text-gray-500">{{ mensaje }}</p>
    </div>
  `,
})
export class LoadingSpinnerComponent {
  @Input() visible = true;
  @Input() diameter = 48;
  @Input() mensaje = '';
}
