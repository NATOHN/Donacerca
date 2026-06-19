import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule } from '@angular/material/dialog';
import { CategoryService } from '../../../services/Category.service';
import { Category } from '../../../models/Category.model';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatTableModule,
    MatSlideToggleModule, MatSnackBarModule, MatDialogModule
  ],
  templateUrl: './categories.html',
  styleUrl: './categories.css'
})
export class CategoriesComponent implements OnInit {
  categories: Category[] = [];
  loading = false;
  showForm = false;
  editingId: string | null = null;
  form: FormGroup;
  displayedColumns = ['name', 'description', 'count', 'status', 'actions'];

  constructor(
    private categoryService: CategoryService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: ['', Validators.maxLength(200)]
    });
  }

  ngOnInit(): void { this.loadCategories(); }

  loadCategories(): void {
    this.loading = true;
    this.categoryService.getAll().subscribe({
      next: (cats) => { this.categories = cats as any; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  openForm(category?: Category): void {
    this.showForm = true;
    if (category) {
      this.editingId = category.id;
      this.form.patchValue({ name: category.name, description: category.description });
    } else {
      this.editingId = null;
      this.form.reset();
    }
  }

  closeForm(): void {
    this.showForm = false;
    this.editingId = null;
    this.form.reset();
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    const data = this.form.value;

    if (this.editingId) {
      this.categoryService.update(this.editingId, data).subscribe({
        next: () => {
          this.snackBar.open('Categoría actualizada', 'OK', { duration: 3000 });
          this.closeForm();
          this.loadCategories();
        },
        error: (err) => this.snackBar.open(err.error?.message || 'Error', 'OK', { duration: 3000 })
      });
    } else {
      this.categoryService.create(data).subscribe({
        next: () => {
          this.snackBar.open('Categoría creada', 'OK', { duration: 3000 });
          this.closeForm();
          this.loadCategories();
        },
        error: (err) => this.snackBar.open(err.error?.message || 'Error', 'OK', { duration: 3000 })
      });
    }
  }

  toggleStatus(category: Category): void {
    this.categoryService.update(category.id, { isActive: !category.isActive }).subscribe({
      next: () => {
        this.snackBar.open(
          `Categoría ${!category.isActive ? 'activada' : 'desactivada'}`,
          'OK', { duration: 3000 }
        );
        this.loadCategories();
      }
    });
  }
}
