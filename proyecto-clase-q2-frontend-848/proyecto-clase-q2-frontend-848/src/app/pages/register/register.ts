import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { AuthService } from '../../services/auth.service';
import { ZoneService } from '../../services/Zone.service';
import { Zone } from '../../models/Zone.model';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatProgressSpinnerModule, MatIconModule,
    MatCheckboxModule, MatSelectModule
  ],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class RegisterComponent implements OnInit {
  form: FormGroup;
  loading = false;
  hidePassword = true;
  errorMessage = '';
  successMessage = '';
  rolesError = false;
  selectedRoles: string[] = [];
  zones: Zone[] = [];

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private zoneService: ZoneService,
    private router: Router
  ) {
    this.form = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      zone: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.zoneService.getAll().subscribe(z => this.zones = z);
  }

  toggleRole(role: string): void {
    if (this.selectedRoles.includes(role)) {
      this.selectedRoles = this.selectedRoles.filter(r => r !== role);
    } else {
      this.selectedRoles.push(role);
    }
    this.rolesError = false;
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    if (this.selectedRoles.length === 0) {
      this.rolesError = true;
      return;
    }
    this.loading = true;
    this.errorMessage = '';

    const data = { ...this.form.value, roles: this.selectedRoles };

    this.authService.register(data).subscribe({
      next: () => {
        this.loading = false;
        this.successMessage = '¡Cuenta creada exitosamente! Iniciando sesión...';

        this.authService.login({ email: data.email, password: data.password }).subscribe({
          next: (res: any) => {
            const roles: string[] = res.User?.Roles ?? res.user?.roles ?? [];
            if (roles.includes('admin')) {
              this.router.navigate(['/admin']);
            } else if (roles.includes('donor')) {
              this.router.navigate(['/donor']);
            } else {
              this.router.navigate(['/receiver']);
            }
          },
          error: () => {
            setTimeout(() => this.router.navigate(['/login']), 1500);
          }
        });
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Error al registrarse';
      }
    });
  }
}
