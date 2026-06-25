import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 0) {
        snackBar.open('Unable to connect to server. Please try again.', 'Close', { duration: 5000 });
      } else if (err.status >= 500) {
        snackBar.open('A server error occurred. Please try again later.', 'Close', { duration: 5000 });
      }
      // 401/403/422 handled by components or jwtInterceptor
      return throwError(() => err);
    })
  );
};
