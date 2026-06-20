import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DonationPostService } from '../../../services/Donation-post.service';
import { CategoryService } from '../../../services/Category.service';
import { ZoneService } from '../../../services/Zone.service';
import { UploadService } from '../../../services/Upload.service';
import { AuthService } from '../../../services/auth.service';
import { Category } from '../../../models/Category.model';
import { Zone } from '../../../models/Zone.model';

@Component({
  selector: 'app-post-form',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './post-form.html',
  styleUrl: './post-form.css'
})
export class PostFormComponent implements OnInit {
  form: FormGroup;
  categories: Category[] = [];
  zones: Zone[] = [];
  loading = false;
  uploadingPhotos = false;
  photoUrls: string[] = [];
  photoLabels: string[][] = [];

  constructor(
    private fb: FormBuilder,
    private postService: DonationPostService,
    private categoryService: CategoryService,
    private zoneService: ZoneService,
    private uploadService: UploadService,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar,
    @Inject(ChangeDetectorRef) private cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      categoryId: ['', Validators.required],
      itemName: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      itemCondition: ['', Validators.required],
      zone: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.categoryService.getAll().subscribe(c => {
      this.categories = c.filter(cat => cat.isActive);
      this.cdr.detectChanges();
    });
    this.zoneService.getAll().subscribe(z => {
      this.zones = z;
      this.cdr.detectChanges();
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const files = Array.from(input.files).slice(0, 5 - this.photoUrls.length);
    if (files.length === 0) return;

    this.uploadingPhotos = true;
    let completed = 0;
    const total = files.length;

    files.forEach(file => {
      this.uploadService.uploadPhoto(file).subscribe({
        next: (res) => {
          this.photoUrls.push(res.url);
          if (res.analysis?.labels) this.photoLabels.push(res.analysis.labels);
          completed++;
          if (completed === total) {
            this.uploadingPhotos = false;
            this.cdr.detectChanges();
          }
        },
        error: (err) => {
          completed++;
          if (completed === total) {
            this.uploadingPhotos = false;
            this.cdr.detectChanges();
          }
          this.snackBar.open(err.error?.message || 'Error al subir foto', 'OK', { duration: 3000 });
        }
      });
    });
  }

  removePhoto(index: number): void {
    this.photoUrls.splice(index, 1);
    this.photoLabels.splice(index, 1);
    this.cdr.detectChanges();
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const data = { ...this.form.value, photoUrls: this.photoUrls };
    this.postService.create(data).subscribe({
      next: () => {
        this.loading = false;
        this.snackBar.open('¡Publicación creada exitosamente!', 'OK', { duration: 3000 });
        this.router.navigate(['/donor/posts']);
      },
      error: (err) => {
        this.loading = false;
        this.cdr.detectChanges();
        this.snackBar.open(err.error?.message || 'Error al crear publicación', 'OK', { duration: 3000 });
      }
    });
  }

  logout(): void {
    this.authService.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
