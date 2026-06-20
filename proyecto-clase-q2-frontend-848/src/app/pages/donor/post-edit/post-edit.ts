import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { forkJoin } from 'rxjs';

import { DonationPostService } from '../../../services/Donation-post.service';
import { CategoryService } from '../../../services/Category.service';
import { ZoneService } from '../../../services/Zone.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-post-edit',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './post-edit.html',
  styleUrl: './post-edit.css'
})
export class PostEditComponent implements OnInit {
  form!: FormGroup;

  categories: any[] = [];
  zones: any[] = [];

  loading = true;
  saving = false;
  postId = '';

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private postService: DonationPostService,
    private categoryService: CategoryService,
    private zoneService: ZoneService,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.postId = this.route.snapshot.paramMap.get('id') || '';

    this.form = this.fb.group({
      itemName: ['', [Validators.required, Validators.minLength(3)]],
      categoryId: ['', Validators.required],
      itemCondition: ['', Validators.required],
      zone: ['', Validators.required],
      description: ['', [Validators.required, Validators.minLength(10)]]
    });

    if (!this.postId) {
      this.loading = false;
      this.snackBar.open('No se encontró la publicación', 'Cerrar', {
        duration: 3000
      });
      this.router.navigate(['/donor/posts']);
      return;
    }

    this.loadData();
  }

  loadData(): void {
    this.loading = true;

    forkJoin({
      post: this.postService.getById(this.postId),
      categories: this.categoryService.getAll(),
      zones: this.zoneService.getAll()
    }).subscribe({
      next: ({ post, categories, zones }: any) => {
        this.categories = categories.filter((c: any) => c.isActive ?? c.IsActive ?? true);
        this.zones = zones;

        this.form.patchValue({
          itemName: post.itemName ?? post.ItemName ?? '',
          categoryId: post.categoryId ?? post.CategoryId ?? '',
          itemCondition: post.itemCondition ?? post.ItemCondition ?? '',
          zone: post.zone ?? post.Zone ?? '',
          description: post.description ?? post.Description ?? ''
        });

        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.snackBar.open('No se pudo cargar la publicación', 'Cerrar', {
          duration: 3000
        });
        this.cdr.detectChanges();
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid || this.saving) {
      return;
    }

    this.saving = true;

    const payload = {
      itemName: this.form.value.itemName,
      categoryId: this.form.value.categoryId,
      itemCondition: this.form.value.itemCondition,
      zone: this.form.value.zone,
      description: this.form.value.description
    };

    this.postService.update(this.postId, payload).subscribe({
      next: () => {
        this.saving = false;
        this.snackBar.open('Publicación actualizada correctamente', 'Cerrar', {
          duration: 3000
        });
        this.router.navigate(['/donor/posts']);
      },
      error: () => {
        this.saving = false;
        this.snackBar.open('No se pudo guardar la publicación', 'Cerrar', {
          duration: 3000
        });
        this.cdr.detectChanges();
      }
    });
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/']),
      error: () => {
        this.authService.clearSession();
        this.router.navigate(['/']);
      }
    });
  }
}