import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, firstValueFrom, tap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface TokenPair {
  accessToken: string;
  refreshToken: string;
}

export interface TmsUser {
  displayName: string;
  email: string;
  role: string;
}

const ACCESS_KEY = 'tms_access_token';
const REFRESH_KEY = 'tms_refresh_token';

// .NET's JwtSecurityTokenHandler emits role/email under the long WS-* claim URIs by
// default, but a cleared/!customised OutboundClaimTypeMap emits the short JWT names
// ("email"/"role") instead. Read both so the decoded user is correct either way.
// FirstName is a custom claim and is never remapped.
const CLAIM_ROLE = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const CLAIM_EMAIL = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';

/** Decode a JWT payload (no verification — display only). Returns null if malformed. */
function decodeJwt(token: string): Record<string, unknown> | null {
  try {
    const payload = token.split('.')[1];
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    // Handle UTF-8 payloads correctly.
    return JSON.parse(decodeURIComponent(escape(json)));
  } catch {
    return null;
  }
}

/**
 * JWT bearer auth against the .NET API (POST /api/auth/login → { accessToken, refreshToken }).
 * Tokens live in localStorage; `currentUser` is a signal derived from the decoded access
 * token so the shell can show the name/role without any raw token touching a template.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  currentUser = signal<TmsUser | null>(this.decodeUser(this.accessToken));
  isAuthenticated = computed(() => this.currentUser() !== null);

  get accessToken(): string | null {
    return localStorage.getItem(ACCESS_KEY);
  }

  private get refreshTokenValue(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  hasRole(role: string): boolean {
    const user = this.currentUser();
    return user?.role === role || user?.role === 'Admin';
  }

  async login(credentials: LoginRequest): Promise<void> {
    const tokens = await firstValueFrom(
      this.http.post<TokenPair>(`${environment.authUrl}/login`, credentials),
    );
    this.setSession(tokens);
  }

  /** Rotate the token pair. Used by the auth interceptor on a 401. */
  refresh(): Observable<TokenPair> {
    const refreshToken = this.refreshTokenValue;
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token'));
    }
    return this.http
      .post<TokenPair>(`${environment.authUrl}/refresh`, { refreshToken })
      .pipe(tap((tokens) => this.setSession(tokens)));
  }

  logout(): void {
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  private setSession(tokens: TokenPair): void {
    localStorage.setItem(ACCESS_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_KEY, tokens.refreshToken);
    this.currentUser.set(this.decodeUser(tokens.accessToken));
  }

  private decodeUser(token: string | null): TmsUser | null {
    if (!token) return null;
    const claims = decodeJwt(token);
    if (!claims) return null;

    // Reject an already-expired token so a stale localStorage entry doesn't look logged in.
    const exp = claims['exp'];
    if (typeof exp === 'number' && exp * 1000 < Date.now()) return null;

    const email = (claims[CLAIM_EMAIL] as string) ?? (claims['email'] as string) ?? '';
    const firstName = (claims['FirstName'] as string) ?? '';
    const rawRole = claims[CLAIM_ROLE] ?? claims['role'];
    const role = Array.isArray(rawRole) ? rawRole[0] : ((rawRole as string) ?? 'User');

    return {
      displayName: firstName || email || 'User',
      email,
      role,
    };
  }
}
