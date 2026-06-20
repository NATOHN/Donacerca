import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { signInWithPopup, GoogleAuthProvider } from 'firebase/auth';
import { firebaseAuth } from '../../firebase.config';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule
  ],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class LoginComponent {
  form: FormGroup;
  loading = false;
  hidePassword = true;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.errorMessage = '';

    this.authService.login(this.form.value).subscribe({
      next: (res) => {
        this.loading = false;
        const user = (res as any).User ?? res.user;
        const roles = (user as any)?.Roles ?? user?.roles ?? [];
        if (roles.includes('admin')) {
          this.router.navigate(['/admin/dashboard']);
        } else {
          this.router.navigate(['/select-role']);
        }
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Error al iniciar sesión';
      }
    });
  }

  loginWithGoogle(): void {
    this.loading = true;
    this.errorMessage = '';

    const provider = new GoogleAuthProvider();
    provider.setCustomParameters({ prompt: 'select_account' });

    signInWithPopup(firebaseAuth, provider)
      .then(result => result.user.getIdToken())
      .then(idToken => {
        this.authService.googleLogin(idToken).subscribe({
          next: (res: any) => {
            this.loading = false;
            const user = res.User ?? res.user;
            const roles = (user as any)?.Roles ?? user?.roles ?? [];
            if (roles.includes('admin')) {
              this.router.navigate(['/admin/dashboard']);
            } else {
              this.router.navigate(['/select-role']);
            }
          },
          error: (err: any) => {
            this.loading = false;
            this.errorMessage = err.error?.message || 'Error con Google';
          }
        });
      })
      .catch(err => {
        this.loading = false;
        this.errorMessage = 'Error al iniciar con Google';
        console.error(err);
      });
  }

  redirectByRole(roles: string[]): void {
    if (roles.includes('admin')) {
      this.router.navigate(['/admin/dashboard']);
    } else if (roles.includes('donor')) {
      this.router.navigate(['/donor/dashboard']);
    } else {
      this.router.navigate(['/receiver/dashboard']);
    }
  }
}
