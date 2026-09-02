import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

/**
 * M12 Session 2 — Unit-testing the JWT auth service.
 *
 * `currentUser` is derived from the access token in the constructor, so each test
 * seeds localStorage before injecting the service. `makeJwt` builds an unsigned
 * token (the service decodes but never verifies) whose claims drive `hasRole`,
 * the Admin override, and the expiry guard. The login test proves the token pair
 * lands in localStorage and the decoded user appears.
 */
describe('AuthService', () => {
  const ACCESS_KEY = 'tms_access_token';

  function makeJwt(claims: Record<string, unknown>, secondsFromNow = 3600): string {
    const header = btoa(JSON.stringify({ alg: 'none', typ: 'JWT' }));
    const payload = btoa(
      JSON.stringify({
        exp: Math.floor(Date.now() / 1000) + secondsFromNow,
        ...claims,
      }),
    );
    return `${header}.${payload}.signature`;
  }

  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('decodes the role and lets an Admin pass any role check', () => {
    localStorage.setItem(ACCESS_KEY, makeJwt({ role: 'Admin', FirstName: 'Root' }));
    const service = TestBed.inject(AuthService);

    expect(service.currentUser()?.role).toBe('Admin');
    expect(service.hasRole('Admin')).toBeTrue();
    expect(service.hasRole('Student')).toBeTrue(); // Admin override
    expect(service.isAuthenticated()).toBeTrue();
  });

  it('grants only the matching role for a non-Admin user', () => {
    localStorage.setItem(ACCESS_KEY, makeJwt({ role: 'Instructor' }));
    const service = TestBed.inject(AuthService);

    expect(service.hasRole('Instructor')).toBeTrue();
    expect(service.hasRole('Student')).toBeFalse();
  });

  it('treats an expired token as signed-out', () => {
    localStorage.setItem(ACCESS_KEY, makeJwt({ role: 'Admin' }, -10));
    const service = TestBed.inject(AuthService);

    expect(service.currentUser()).toBeNull();
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('login() stores the token pair and exposes the decoded user', async () => {
    const service = TestBed.inject(AuthService);

    const done = service.login({ email: 'admin@tms.local', password: 'pw' });

    const req = httpMock.expectOne(`${environment.authUrl}/login`);
    expect(req.request.method).toBe('POST');
    req.flush({
      accessToken: makeJwt({ role: 'Admin', FirstName: 'Root' }),
      refreshToken: 'refresh-token',
    });
    await done;

    expect(localStorage.getItem(ACCESS_KEY)).toBeTruthy();
    expect(service.currentUser()?.role).toBe('Admin');
    expect(service.currentUser()?.displayName).toBe('Root');
  });
});
