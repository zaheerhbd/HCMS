export interface LoginRequest {
  username: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  username: string;
  roles: string[];
}

export interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  username: string | null;
  roles: string[];
  isAuthenticated: boolean;
}
