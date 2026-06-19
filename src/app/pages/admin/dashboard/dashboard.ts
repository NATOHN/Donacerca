import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../../services/Report.service';
import { AuthService } from '../../../services/auth.service';
import { DashboardStats } from '../../../models/Report.model';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive, FormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatSelectModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class AdminDashboardComponent implements OnInit {
  stats: DashboardStats | null = null;
  trend: any[] = [];
  loading = true;
  trendPeriod: 'week' | 'month' = 'week';
  userName = '';

  constructor(
    private reportService: ReportService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userName = this.authService.getCurrentUser()?.fullName || 'Admin';
    this.loadData();
    this.loadTrend();
  }

  loadData(): void {
    this.loading = true;
    this.reportService.getDashboard().subscribe({
      next: (stats) => { this.stats = stats; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  loadTrend(): void {
    this.reportService.getTrend(this.trendPeriod).subscribe({
      next: (t) => { this.trend = t; },
      error: () => {}
    });
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => {
        this.authService.clearSession();
        this.router.navigate(['/login']);
      }
    });
  }

  getStatusEntries(): { key: string, value: number }[] {
    if (!this.stats?.byStatus) return [];
    return Object.entries(this.stats.byStatus)
      .map(([key, value]) => ({ key, value }));
  }
}
