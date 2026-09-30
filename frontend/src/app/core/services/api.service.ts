import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiResponse, PaginationResult } from '../../shared/models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  readonly baseUrl = environment.apiUrl;

  get<T>(path: string, params?: Record<string, string | number | boolean>): Observable<T> {
    let httpParams = new HttpParams();
    if (params) {
      Object.entries(params).forEach(([k, v]) => {
        if (v !== null && v !== undefined) {
          httpParams = httpParams.set(k, String(v));
        }
      });
    }
    return this.http
      .get<ApiResponse<T>>(`${this.baseUrl}/${path}`, { params: httpParams })
      .pipe(
        map(res => this.unwrap(res)),
        catchError(this.handleError),
      );
  }

  getPaginated<T>(
    path: string,
    params?: Record<string, string | number | boolean>,
  ): Observable<PaginationResult<T>> {
    return this.get<PaginationResult<T>>(path, params);
  }

  post<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .post<ApiResponse<T>>(`${this.baseUrl}/${path}`, body)
      .pipe(
        map(res => this.unwrap(res)),
        catchError(this.handleError),
      );
  }

  put<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .put<ApiResponse<T>>(`${this.baseUrl}/${path}`, body)
      .pipe(
        map(res => this.unwrap(res)),
        catchError(this.handleError),
      );
  }

  patch<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .patch<ApiResponse<T>>(`${this.baseUrl}/${path}`, body)
      .pipe(
        map(res => this.unwrap(res)),
        catchError(this.handleError),
      );
  }

  delete<T>(path: string): Observable<T> {
    return this.http
      .delete<ApiResponse<T>>(`${this.baseUrl}/${path}`)
      .pipe(
        map(res => this.unwrap(res)),
        catchError(this.handleError),
      );
  }

  private unwrap<T>(res: ApiResponse<T>): T {
    if (!res.exitoso) {
      throw new Error(res.mensaje ?? 'Error desconocido del servidor');
    }
    return res.contenido;
  }

  private handleError(error: unknown): Observable<never> {
    if (error instanceof Error) {
      return throwError(() => error);
    }
    const httpError = error as { status?: number; error?: { mensaje?: string } };
    const mensaje =
      httpError?.error?.mensaje ??
      `Error ${httpError?.status ?? 'desconocido'} al comunicarse con el servidor`;
    return throwError(() => new Error(mensaje));
  }
}
