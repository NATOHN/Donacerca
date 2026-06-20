import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getToken();
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // No intentar renovar si es logout, login o refresh
      const isAuthUrl = req.url.includes('/Auth/logout') ||
        req.url.includes('/Auth/login') ||
        req.url.includes('/Auth/refresh') ||
        req.url.includes('/Auth/register');

      if (error.status === 401 && !isAuthUrl) {
        return authService.refresh().pipe(
          switchMap(res => {
            const retryReq = req.clone({
              setHeaders: { Authorization: `Bearer ${res.token}` }
            });
            return next(retryReq);
          }),
          catchError(() => {
            authService.clearSession();
            router.navigate(['/login']);
            return throwError(() => error);
          })
        );
      }

      // Si logout falla por 401, limpiar sesión igual
      if (error.status === 401 && req.url.includes('/Auth/logout')) {
        authService.clearSession();
        router.navigate(['/login']);
      }

      return throwError(() => error);
    })
  );
};
