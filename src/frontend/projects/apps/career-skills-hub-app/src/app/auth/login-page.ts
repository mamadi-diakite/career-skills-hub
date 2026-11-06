import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService, FormSubmission, safeReturnUrl } from '@career-skills-hub/shared/core';
import { FieldErrors } from '@career-skills-hub/shared/ui';

@Component({
  selector: 'csh-login-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <form class="form-card" [formGroup]="form" (ngSubmit)="logIn()" novalidate>
      <h1 class="text-2xl font-bold tracking-tight">Log in</h1>

      @if (expired) {
        <p class="alert-info" role="status">Your session has expired. Please log in again.</p>
      }
      @if (submission.errors().form; as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      <div>
        <label class="form-label" for="email">Email</label>
        <input
          id="email"
          class="form-input"
          type="email"
          formControlName="email"
          autocomplete="email"
        />
        <csh-field-errors [messages]="submission.messagesFor('email')" />
      </div>

      <div>
        <label class="form-label" for="password">Password</label>
        <input
          id="password"
          class="form-input"
          type="password"
          formControlName="password"
          autocomplete="current-password"
        />
        <csh-field-errors [messages]="submission.messagesFor('password')" />
      </div>

      <button class="button-primary" type="submit" [disabled]="submission.pending()">Log in</button>

      <p class="text-center text-sm text-slate-600">
        New here?
        <a routerLink="/sign-up" class="font-medium text-teal-700">Create your company</a>
      </p>
    </form>
  `,
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly query = inject(ActivatedRoute).snapshot.queryParamMap;

  protected readonly expired = this.query.has('expired');
  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  protected readonly submission = new FormSubmission(this.form, {
    email: 'Enter a valid email address.',
    password: 'Enter your password.',
  });

  protected logIn(): void {
    this.submission.submit(
      () => this.auth.login(this.form.getRawValue()),
      () => void this.router.navigateByUrl(safeReturnUrl(this.query.get('returnUrl'))),
    );
  }
}
