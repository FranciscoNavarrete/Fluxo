export interface ApiResponse<T> {
  exitoso: boolean;
  mensaje: string;
  contenido: T;
}

export type RespuestaResultado<T> = ApiResponse<T>;

export interface PaginationResult<T> {
  items: T[];
  totalItems: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiError {
  status: number;
  mensaje: string;
  errores?: string[];
}
