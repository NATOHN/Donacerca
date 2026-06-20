import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-select-role',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './select-role.html',
  styleUrl: './select-role.css'
})
export class SelectRoleComponent {
  loading = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
    }
  }

  selectRole(role: string): void {
    this.loading = true;

    this.authService.updateProfile({ roles: [role] }).subscribe({
      next: () => {
        this.loading = false;
        setTimeout(() => {
          if (role === 'donor') {
            this.router.navigate(['/donor/dashboard']);
          } else {
            this.router.navigate(['/receiver/dashboard']);
          }
        }, 100);
      },
      error: () => {
        this.loading = false;
        if (role === 'donor') {
          this.router.navigate(['/donor/dashboard']);
        } else {
          this.router.navigate(['/receiver/dashboard']);
        }
      }
    });
  }
}
