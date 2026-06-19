import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatBadgeModule } from '@angular/material/badge';
import { DonationRequestService } from '../../../services/Donation-request.service';
import { NotificationService } from '../../../services/Notification.service';
import { AuthService } from '../../../services/auth.service';
import { DonationRequest } from '../../../models/Donation-request.model';
import { Notification } from '../../../models/Notification.model';

@Component({
  selector: 'app-receiver-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatBadgeModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class ReceiverDashboardComponent implements OnInit {
  requests: DonationRequest[] = [];
  notifications: Notification[] = [];
  loading = true;
  userName = '';

  get pendingRequests() { return this.requests.filter(r => r.status === 'pendiente').length; }
  get acceptedRequests() { return this.requests.filter(r => r.status === 'aceptada').length; }
  get unreadNotifications() { return this.notifications.filter(n => !n.isRead).length; }

  constructor(
    private requestService: DonationRequestService,
    private notificationService: NotificationService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userName = this.authService.getCurrentUser()?.fullName || 'Receptor';
    this.loadData();
  }

  loadData(): void {
    this.requestService.getMine().subscribe({
      next: (reqs) => { this.requests = reqs; this.loading = false; },
      error: () => { this.loading = false; }
    });
    this.notificationService.getMine().subscribe({
      next: (notifs) => this.notifications = notifs,
      error: () => {}
    });
  }

  markAsRead(id: string): void {
    this.notificationService.markAsRead(id).subscribe(() => {
      const notif = this.notifications.find(n => n.id === id);
      if (notif) notif.isRead = true;
    });
  }

  logout(): void {
    this.authService.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
