import { Component, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import {
  ApiHealthService,
  AuthService,
  roleLabel,
  SessionStore,
} from '@career-skills-hub/shared/core';
import { Shell } from '@career-skills-hub/shared/ui';

@Component({
  selector: 'csh-root',
  imports: [RouterOutlet, RouterLink, Shell],
  template: `
    <csh-shell appName="Career Skills Hub" [apiStatus]="apiStatus()">
      <ng-container shellNav>
        @if (user(); as user) {
          <a routerLink="/employees" class="text-teal-700">Employees</a>
          <a routerLink="/jobs" class="text-teal-700">Jobs</a>
          <a routerLink="/skills" class="text-teal-700">Skills</a>
          <a routerLink="/departments" class="text-teal-700">Departments</a>
          <a routerLink="/company" class="text-teal-700">Company</a>
          <a routerLink="/account/password" class="text-teal-700">Change password</a>
          <span class="text-slate-600" data-testid="user-name"
            >{{ user.displayName }} · {{ roleLabel(user.role) }}</span
          >
          <button type="button" class="text-teal-700" (click)="logOut()">Log out</button>
        } @else {
          <a routerLink="/login" class="text-teal-700">Log in</a>
          <a routerLink="/sign-up" class="text-teal-700">Create your company</a>
        }
      </ng-container>
      <router-outlet />
    </csh-shell>
  `,
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly session = inject(SessionStore);

  protected readonly roleLabel = roleLabel;
  protected readonly user = this.session.user;
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });

  constructor() {
    effect(() => {
      if (this.session.expired()) {
        void this.router.navigate(['/login'], { queryParams: { expired: true } });
      }
    });
  }

  protected logOut(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
