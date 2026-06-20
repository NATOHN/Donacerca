import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { DonationRequestService } from '../../../services/Donation-request.service';
import { NotificationService } from '../../../services/Notification.service';
import { DonationPostService } from '../../../services/Donation-post.service';
import { AuthService } from '../../../services/auth.service';
import { DonationRequest } from '../../../models/Donation-request.model';

@Component({
  selector: 'app-receiver-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatBadgeModule, MatMenuModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class ReceiverDashboardComponent implements OnInit {
  requests:      DonationRequest[] = [];
  notifications: any[]             = [];
  postNames:  { [postId: string]: string } = {};
  postPhotos: { [postId: string]: string } = {};
  loading  = true;
  userName = '';

  statsAvailable = 0;
  statsReserved  = 0;
  statsDelivered = 0;
  statsTotal     = 0;

  get pendingRequests()     { return this.requests.filter(r => r.status === 'pendiente').length; }
  get acceptedRequests()    { return this.requests.filter(r => r.status === 'aceptada').length; }
  get unreadCount()         { return this.notifications.filter(n => !n.isRead && !n.IsRead).length; }

  constructor(
    private requestService:      DonationRequestService,
    private notificationService: NotificationService,
    private postService:         DonationPostService,
    private authService:         AuthService,
    private router:              Router,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const user    = this.authService.getCurrentUser();
    this.userName = (user as any)?.FullName ?? user?.fullName ?? 'Receptor';
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    forkJoin({
      requests:      this.requestService.getMine(),
      notifications: this.notificationService.getMine(),
      stats:         this.postService.getReceiverStats().pipe(catchError(() => of(null)))
    }).subscribe({
      next: ({ requests, notifications, stats }) => {
        this.requests      = requests;
        this.notifications = notifications;

        if (stats) {
          this.statsAvailable = stats.available ?? stats.Available ?? 0;
          this.statsReserved  = stats.reserved  ?? stats.Reserved  ?? 0;
          this.statsDelivered = stats.delivered ?? stats.Delivered ?? 0;
          this.statsTotal     = stats.total     ?? stats.Total     ?? 0;
        }

        const uniquePostIds = [...new Set(requests.slice(0, 5).map(r => r.postId))];
        if (uniquePostIds.length === 0) {
          this.loading = false;
          this.cdr.detectChanges();
          return;
        }

        const postRequests = uniquePostIds.map(postId =>
          this.postService.getById(postId).pipe(catchError(() => of(null)))
        );

        forkJoin(postRequests).subscribe({
          next: (posts: any[]) => {
            posts.forEach((post, index) => {
              const postId = uniquePostIds[index];
              if (post) {
                this.postNames[postId]  = post.itemName ?? post.ItemName ?? 'Sin nombre';
                const photos            = post.photoUrls ?? post.PhotoUrls ?? [];
                this.postPhotos[postId] = photos.length > 0 ? photos[0] : '';
              } else {
                this.postNames[postId]  = 'Artículo eliminado';
                this.postPhotos[postId] = '';
              }
            });
            this.loading = false;
            this.cdr.detectChanges();
          },
          error: () => { this.loading = false; this.cdr.detectChanges(); }
        });
      },
      error: () => { this.loading = false; this.cdr.detectChanges(); }
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
      'selected':  'volunteer_activism',
      'delivered': 'local_shipping',
      'expired':   'schedule',
      'request':   'inbox',
      'completed': 'check_circle'
    };
    return icons[type] || 'notifications';
  }

  logout(): void {
    this.authService.logout().subscribe({
      next:  () => this.router.navigate(['/login']),
      error: () => {
        this.authService.clearSession();
        this.router.navigate(['/login']);
      }
    });
  }
}
