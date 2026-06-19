export interface User {
  id: string;
  fullName: string;
  email: string;
  zone: string;
  roles: string[];
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  zone: string;
  roles: string[];
}
