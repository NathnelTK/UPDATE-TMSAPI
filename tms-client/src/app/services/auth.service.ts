import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TmsUser {
  displayName: string;
  role: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

// NOTE: @Service() does not exist in Angular — the PDF's snippet is wrong.
// The correct decorator is @Injectable({ providedIn: 'root' }), which registers
// one app-wide singleton (the DI analogue of AddSingleton<T>() in .NET).
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/auth`;

  // Session state as a signal — components read currentUser() reactively without
  // any raw token ever touching the DOM (the token lives only in the HttpOnly cookie).
  currentUser = signal<TmsUser | null>(null);

  hasRole(role: string): boolean {
    const user = this.currentUser();
    return user?.role === role || user?.role === 'Admin';
  }

  async login(credentials: LoginRequest) {
    // The server sets the HttpOnly cookie via the Set-Cookie response header.
    await firstValueFrom(this.http.post<TmsUser>(`${this.base}/login`, credentials));

    // Fetch the authenticated profile — the browser sends the cookie automatically.
    const user = await firstValueFrom(this.http.get<TmsUser>(`${this.base}/me`));
    this.currentUser.set(user);
  }
}
