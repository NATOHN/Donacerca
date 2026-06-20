import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { forkJoin } from 'rxjs';
import { DonationPostService } from '../../../services/Donation-post.service';
import { DonationRequestService } from '../../../services/Donation-request.service';
import { CategoryService } from '../../../services/Category.service';
import { ZoneService } from '../../../services/Zone.service';
import { AuthService } from '../../../services/auth.service';
import { DonationPost } from '../../../models/Donation-post.model';
import { Category } from '../../../models/Category.model';
import { Zone } from '../../../models/Zone.model';

@Component({
  selector: 'app-browse',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive, FormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatSelectModule,
    MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './browse.html',
  styleUrl: './browse.css'
})
export class BrowseComponent implements OnInit {
  posts: DonationPost[] = [];
  categories: Category[] = [];
  zones: Zone[] = [];
  loading = true;
  requesting: { [key: string]: boolean } = {};
  selectedCategory = '';
  selectedZone = '';

  constructor(
    private postService: DonationPostService,
    private requestService: DonationRequestService,
    private categoryService: CategoryService,
    private zoneService: ZoneService,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    forkJoin({
      categories: this.categoryService.getAll(),
      zones: this.zoneService.getAll()
    }).subscribe({
      next: ({ categories, zones }) => {
        this.categories = categories;
        this.zones = zones;
        this.cdr.detectChanges();
      }
    });
    this.loadPosts();
  }

  loadPosts(): void {
    this.loading = true;
    this.postService.getAvailable(
      this.selectedCategory || undefined,
      this.selectedZone || undefined
    ).subscribe({
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

  clearFilters(): void {
    this.selectedCategory = '';
    this.selectedZone = '';
    this.loadPosts();
  }

  requestItem(postId: string): void {
    this.requesting[postId] = true;
    this.requestService.create(postId).subscribe({
      next: () => {
        this.requesting[postId] = false;
        this.cdr.detectChanges();
        this.snackBar.open('¡Solicitud enviada!', 'OK', { duration: 3000 });
        this.router.navigate(['/receiver/my-requests']);
      },
      error: (err) => {
        this.requesting[postId] = false;
        this.cdr.detectChanges();
        this.snackBar.open(err.error?.message || 'Error al solicitar', 'OK', { duration: 3000 });
      }
    });
  }

  logout(): void {
    this.authService.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
