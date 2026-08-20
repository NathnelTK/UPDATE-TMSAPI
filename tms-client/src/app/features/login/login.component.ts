import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';

/**
 * Split-panel sign-in. Left: brand/marketing. Right: reactive login form wired to
 * AuthService (JWT). On success it honours a `returnUrl` query param (set by the
 * auth guard) or falls back to the dashboard. Demo credentials are prefilled and
 * one-click fillable so the app is testable out of the box.
 */
@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loading = signal(false);
  error = signal<string | null>(null);
  showPassword = signal(false);

  form = this.fb.nonNullable.group({
    email: ['admin@tms.local', [Validators.required, Validators.email]],
    password: ['Demo!Passw0rd2026', [Validators.required]],
  });

  togglePassword(): void {
    this.showPassword.update((v) => !v);
  }

  fillDemo(role: 'admin' | 'student'): void {
    this.form.setValue({
      email: role === 'admin' ? 'admin@tms.local' : 'student@tms.local',
      password: 'Demo!Passw0rd2026',
    });
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    try {
      await this.auth.login(this.form.getRawValue());
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
      await this.router.navigateByUrl(returnUrl);
    } catch (err: unknown) {
      this.error.set(this.messageFor(err));
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Turn a failed login into a human message. Status 0 means the request never reached
   * the API (backend down, started on the wrong port, or an untrusted HTTPS dev cert) —
   * that is a connection problem, NOT a bad password, so don't blame the credentials.
   */
  private messageFor(err: unknown): string {
    const e = err as { status?: number; error?: { detail?: string } };
    if (e?.status === 0) {
      return 'Cannot reach the server at https://localhost:7190 — is the backend API running?';
    }
    if (e?.status === 401) {
      return 'Invalid email or password. Please try again.';
    }
    return e?.error?.detail ?? 'Something went wrong. Please try again.';
  }
}
