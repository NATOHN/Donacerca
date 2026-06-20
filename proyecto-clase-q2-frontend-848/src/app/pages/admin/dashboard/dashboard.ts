import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Chart, registerables } from 'chart.js';
import { ReportService } from '../../../services/Report.service';
import { AuthService } from '../../../services/auth.service';
import { CategoryService } from '../../../services/Category.service';
import { ZoneService } from '../../../services/Zone.service';
import { DashboardStats } from '../../../models/Report.model';

Chart.register(...registerables);

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive, FormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatSelectModule,
    MatFormFieldModule, MatInputModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class AdminDashboardComponent implements OnInit {
  stats: DashboardStats | null = null;
  trend: any[] = [];
  categories: any[] = [];
  zones: any[] = [];
  loading = true;
  trendPeriod: 'week' | 'month' = 'week';
  userName = '';

  filterCategory = '';
  filterZone = '';
  filterFrom = '';
  filterTo = '';

  private pieChart: Chart | null = null;
  private barChart: Chart | null = null;
  private trendChart: Chart | null = null;

  constructor(
    private reportService: ReportService,
    private authService: AuthService,
    private categoryService: CategoryService,
    private zoneService: ZoneService,
    private router: Router,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    this.userName = (user as any)?.FullName ?? user?.fullName ?? 'Admin';

    forkJoin({
      cats: this.categoryService.getAll(),
      zones: this.zoneService.getAll()
    }).subscribe({
      next: ({ cats, zones }: any) => {
        this.categories = cats;
        this.zones = zones;
        this.cdr.detectChanges();
      }
    });

    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    forkJoin({
      stats: this.reportService.getDashboard(
        this.filterCategory || undefined,
        this.filterZone || undefined,
        this.filterFrom || undefined,
        this.filterTo || undefined
      ),
      trend: this.reportService.getTrend(this.trendPeriod)
    }).subscribe({
      next: ({ stats, trend }) => {
        this.stats = stats;
        this.trend = trend;
        this.loading = false;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.renderPieChart();
          this.renderBarChart();
          this.renderTrendChart();
        }, 100);
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadTrend(): void {
    this.reportService.getTrend(this.trendPeriod).subscribe({
      next: (t) => {
        this.trend = t;
        this.cdr.detectChanges();
        setTimeout(() => this.renderTrendChart(), 100);
      }
    });
  }

  applyFilters(): void {
    console.log('Filtros:', {
      category: this.filterCategory,
      zone: this.filterZone,
      from: this.filterFrom,
      to: this.filterTo
    });
    this.loadData();
  }

  clearFilters(): void {
    this.filterCategory = '';
    this.filterZone = '';
    this.filterFrom = '';
    this.filterTo = '';
    this.loadData();
  }

  private renderPieChart(): void {
    const canvas = document.getElementById('pieChart') as HTMLCanvasElement;
    if (!canvas || !this.stats?.byStatus) return;
    if (this.pieChart) this.pieChart.destroy();

    const entries = Object.entries(this.stats.byStatus);
    this.pieChart = new Chart(canvas, {
      type: 'doughnut',
      data: {
        labels: entries.map(([k]) => k.charAt(0).toUpperCase() + k.slice(1)),
        datasets: [{
          data: entries.map(([, v]) => v),
          backgroundColor: ['#4caf50', '#ff9800', '#2196f3', '#9e9e9e'],
          borderWidth: 2
        }]
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'bottom' } }
      }
    });
  }

  private renderBarChart(): void {
    const canvas = document.getElementById('barChart') as HTMLCanvasElement;
    if (!canvas || !this.stats?.byCategory) return;
    if (this.barChart) this.barChart.destroy();

    const entries = Object.entries(this.stats.byCategory);
    this.barChart = new Chart(canvas, {
      type: 'bar',
      data: {
        labels: entries.map(([k]) => k),
        datasets: [{
          label: 'Artículos por categoría',
          data: entries.map(([, v]) => v),
          backgroundColor: '#e91e63',
          borderRadius: 6
        }]
      },
      options: {
        responsive: true,
        plugins: { legend: { display: false } },
        scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
      }
    });
  }

  private renderTrendChart(): void {
    const canvas = document.getElementById('trendChart') as HTMLCanvasElement;
    if (!canvas || !this.trend.length) return;
    if (this.trendChart) this.trendChart.destroy();

    this.trendChart = new Chart(canvas, {
      type: 'line',
      data: {
        labels: this.trend.map(t => t.date ?? t.month ?? ''),
        datasets: [{
          label: 'Donaciones completadas',
          data: this.trend.map(t => t.count),
          borderColor: '#e91e63',
          backgroundColor: 'rgba(233,30,99,0.1)',
          fill: true,
          tension: 0.4
        }]
      },
      options: {
        responsive: true,
        plugins: { legend: { display: false } },
        scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
      }
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
    return Object.entries(this.stats.byStatus).map(([key, value]) => ({ key, value }));
  }

  getCategoryEntries(): { key: string, value: number }[] {
    if (!this.stats?.byCategory) return [];
    return Object.entries(this.stats.byCategory)
      .map(([key, value]) => ({ key, value }))
      .sort((a, b) => b.value - a.value);
  }
}
