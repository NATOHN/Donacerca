import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { NotificationService } from '../../../services/Notification.service';
import { DonationPostService } from '../../../services/Donation-post.service';
import { AuthService } from '../../../services/auth.service';
import { DonationPost } from '../../../models/Donation-post.model';

@Component({
  selector: 'app-donor-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatBadgeModule, MatMenuModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DonorDashboardComponent implements OnInit {
  posts: DonationPost[] = [];
  notifications: any[] = [];
  loading = true;
  userName = '';

  get activePosts() { return this.posts.filter(p => p.status === 'disponible').length; }
  get reservedPosts() { return this.posts.filter(p => p.status === 'reservado').length; }
  get completedPosts() { return this.posts.filter(p => p.status === 'entregado').length; }
  get unreadCount() { return this.notifications.filter(n => !n.isRead && !n.IsRead).length; }

  constructor(
    private postService: DonationPostService,
    private notificationService: NotificationService,
    private authService: AuthService,
    private router: Router,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    this.userName = (user as any)?.FullName ?? user?.fullName ?? 'Donante';
    this.loadPosts();
    this.loadNotifications();
  }

  loadPosts(): void {
    this.postService.getMine().subscribe({
      next: (posts) => {
        this.posts = posts;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadNotifications(): void {
    this.notificationService.getMine().subscribe({
      next: (notifs) => {
        this.notifications = notifs;
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  markAsRead(id: string): void {
    this.notificationService.markAsRead(id).subscribe({
      next: () => {
        const n = this.notifications.find(x => x.id === id || x.Id === id);
        if (n) { n.isRead = true; n.IsRead = true; }
        this.cdr.detectChanges();
      }
    });
  }

  getNotificationIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'expired': 'schedule',
      'selected': 'person',
      'delivered': 'local_shipping',
      'request': 'inbox',
      'completed': 'volunteer_activism'
    };
    return icons[type] || 'notifications';
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
}
