import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';
import { donorGuard } from './guards/donor.guard';
import { receiverGuard } from './guards/receiver.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/landing/landing').then(m => m.LandingComponent) },
  { path: 'select-role', loadComponent: () => import('./pages/select-role/select-role').then(m => m.SelectRoleComponent) },
  { path: 'login', loadComponent: () => import('./pages/login/login').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./pages/register/register').then(m => m.RegisterComponent) },
  { path: 'catalog', loadComponent: () => import('./pages/catalog/catalog').then(m => m.CatalogComponent) },
  { path: 'catalog/:id', loadComponent: () => import('./pages/catalog/Item-detail').then(m => m.ItemDetailComponent) },
  { path: 'unauthorized', loadComponent: () => import('./pages/unauthorized/unauthorized').then(m => m.UnauthorizedComponent) },
  { path: 'forgot-password', loadComponent: () => import('./pages/forgot-password/forgot-password').then(m => m.ForgotPasswordComponent) },
  { path: 'reset-password', loadComponent: () => import('./pages/reset-password/reset-password').then(m => m.ResetPasswordComponent) },

  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./pages/admin/dashboard/dashboard').then(m => m.AdminDashboardComponent) },
      { path: 'categories', loadComponent: () => import('./pages/admin/categories/categories').then(m => m.CategoriesComponent) },
      { path: 'moderation', loadComponent: () => import('./pages/admin/moderation/moderation').then(m => m.ModerationComponent) },
      { path: 'history', loadComponent: () => import('./pages/admin/history/history').then(m => m.HistoryComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  {
    path: 'donor',
    canActivate: [authGuard, donorGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./pages/donor/dashboard/dashboard').then(m => m.DonorDashboardComponent) },
      { path: 'posts', loadComponent: () => import('./pages/donor/posts/posts').then(m => m.PostsComponent) },
      { path: 'posts/new', loadComponent: () => import('./pages/donor/post-form/post-form').then(m => m.PostFormComponent) },
      { path: 'posts/edit/:id', loadComponent: () => import('./pages/donor/post-edit/post-edit').then(m => m.PostEditComponent) },
      { path: 'requests/:postId', loadComponent: () => import('./pages/donor/requests/requests').then(m => m.RequestsComponent) },
      { path: 'delivery/:postId', loadComponent: () => import('./pages/donor/delivery/delivery').then(m => m.DeliveryComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  {
    path: 'receiver',
    canActivate: [authGuard, receiverGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./pages/receiver/dashboard/dashboard').then(m => m.ReceiverDashboardComponent) },
      { path: 'browse', loadComponent: () => import('./pages/receiver/browse/browse').then(m => m.BrowseComponent) },
      { path: 'my-requests', loadComponent: () => import('./pages/receiver/my-requests/my-requests').then(m => m.MyRequestsComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  { path: '**', redirectTo: '' }
];
