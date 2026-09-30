import {
  Component,
  Input,
  Output,
  EventEmitter,
  ContentChildren,
  ViewChild,
  QueryList,
  AfterContentInit,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatColumnDef, MatTable } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { LoadingSpinnerComponent } from '../loading-spinner/loading-spinner.component';

export interface TablaPaginadaConfig {
  columnas: string[];
  totalItems: number;
  pageSize?: number;
  pageSizeOptions?: number[];
  sortActivo?: string;
  sortDireccion?: 'asc' | 'desc';
}

export interface CambioTabla {
  pageNumber: number;
  pageSize: number;
  sort?: string;
  direction?: 'asc' | 'desc';
}

@Component({
  selector: 'app-tabla-paginada',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatProgressBarModule,
    LoadingSpinnerComponent,
  ],
  template: `
    <div class="relative overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
      <mat-progress-bar *ngIf="cargando" mode="indeterminate" class="absolute top-0 left-0 right-0" />

      <app-loading-spinner
        *ngIf="cargando && !datos?.length"
        [visible]="true"
        mensaje="Cargando datos..."
      />

      <div *ngIf="!cargando && !datos?.length" class="flex flex-col items-center justify-center py-16 text-gray-400">
        <svg class="w-12 h-12 mb-3 opacity-40" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
            d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
        </svg>
        <p class="text-sm">Sin resultados</p>
      </div>

      <div [class.opacity-50]="cargando" class="overflow-x-auto">
        <table
          mat-table
          matSort
          [dataSource]="datos ?? []"
          [matSortActive]="config.sortActivo ?? ''"
          [matSortDirection]="config.sortDireccion ?? 'asc'"
          (matSortChange)="onSort($event)"
          class="w-full"
        >
          <ng-content />

          <tr mat-header-row *matHeaderRowDef="config.columnas; sticky: true"
              class="bg-gray-50"></tr>
          <tr
            mat-row
            *matRowDef="let row; columns: config.columnas"
            class="hover:bg-blue-50 transition-colors cursor-pointer"
            (click)="filaClick.emit(row)"
          ></tr>
        </table>
      </div>

      <mat-paginator
        *ngIf="config.totalItems > 0"
        [length]="config.totalItems"
        [pageSize]="config.pageSize ?? 10"
        [pageSizeOptions]="config.pageSizeOptions ?? [5, 10, 25, 50]"
        [pageIndex]="paginaActual"
        (page)="onPage($event)"
        showFirstLastButtons
        class="border-t border-gray-100"
      />
    </div>
  `,
  styles: [`
    table { width: 100%; }
    th.mat-header-cell { font-weight: 600; color: #374151; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; }
    td.mat-cell { font-size: 0.875rem; color: #1f2937; }
    tr.mat-row { border-bottom: 1px solid #f3f4f6; }
  `],
})
export class TablaPaginadaComponent<T> implements AfterContentInit, OnChanges {
  @Input({ required: true }) config!: TablaPaginadaConfig;
  @Input() datos: T[] | null = [];
  @Input() cargando = false;

  @Output() cambio    = new EventEmitter<CambioTabla>();
  @Output() filaClick = new EventEmitter<T>();

  @ContentChildren(MatColumnDef) columnDefs!: QueryList<MatColumnDef>;
  @ViewChild(MatTable, { static: true }) table!: MatTable<T>;

  paginaActual = 0;
  private sortActual: Sort = { active: '', direction: '' };

  ngAfterContentInit(): void {
    this.columnDefs.forEach(def => this.table.addColumnDef(def));
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['config']) {
      this.paginaActual = 0;
    }
  }

  onSort(sort: Sort): void {
    this.sortActual = sort;
    this.paginaActual = 0;
    this.emitir();
  }

  onPage(event: PageEvent): void {
    this.paginaActual = event.pageIndex;
    this.emitir(event.pageSize);
  }

  private emitir(pageSize?: number): void {
    this.cambio.emit({
      pageNumber: this.paginaActual + 1,
      pageSize:   pageSize ?? this.config.pageSize ?? 10,
      sort:       this.sortActual.active || undefined,
      direction:  (this.sortActual.direction as 'asc' | 'desc') || undefined,
    });
  }
}
