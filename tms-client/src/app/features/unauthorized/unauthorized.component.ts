import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * M11 Session 3 - Exercise 6: the landing page for role-guarded routes the user
 * isn't allowed to see. roleGuard('Admin') redirects here (not to /login) because
 * the visitor is authenticated — they just lack the required role.
 */
@Component({
  selector: 'app-unauthorized',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <section class="denied">
      <span class="material-icons denied-icon">gpp_maybe</span>
      <h1>Access denied</h1>
      <p>
        Your account doesn't have permission to view this area. If you believe this
        is a mistake, contact an administrator.
      </p>
      <a class="btn" routerLink="/dashboard">
        <span class="material-icons">arrow_back</span>
        Back to dashboard
      </a>
    </section>
  `,
  styles: [
    `
      .denied {
        max-width: 32rem;
        margin: 4rem auto;
        padding: 0 1rem;
        text-align: center;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 1rem;
      }
      .denied-icon {
        font-size: 4rem;
        color: var(--warn, #d97706);
      }
      h1 {
        margin: 0;
        font-size: 1.75rem;
      }
      p {
        color: var(--text-muted, #6b7280);
        line-height: 1.6;
        margin: 0;
      }
      .btn {
        display: inline-flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.6rem 1.1rem;
        border-radius: 0.6rem;
        background: var(--accent, #4f46e5);
        color: #fff;
        text-decoration: none;
        font-weight: 600;
      }
      .btn:hover {
        filter: brightness(1.05);
      }
    `,
  ],
})
export class UnauthorizedComponent {}
