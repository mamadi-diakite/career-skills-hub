import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, FormSubmission } from '@career-skills-hub/shared/core';
import { FieldErrors } from '@career-skills-hub/shared/ui';

@Component({
  selector: 'csh-sign-up-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <form class="form-card" [formGroup]="form" (ngSubmit)="signUp()" novalidate>
      <h1 class="text-2xl font-bold tracking-tight">Create your company</h1>

      @if (submission.errors().form; as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      <p class="text-sm text-slate-600">
        You become the first HR admin of the company and can then add jobs, skills and employees.
      </p>

      <div>
        <label class="form-label" for="companyName">Company name</label>
        <input
          id="companyName"
          class="form-input"
          formControlName="companyName"
          autocomplete="organization"
        />
        <csh-field-errors [messages]="submission.messagesFor('companyName')" />
      </div>

      <div>
        <label class="form-label" for="headcount">Headcount</label>
        <input
          id="headcount"
          class="form-input"
          type="number"
          min="1"
          formControlName="headcount"
        />
        <csh-field-errors [messages]="submission.messagesFor('headcount')" />
      </div>

      <div>
        <label class="form-label" for="displayName">Your name</label>
        <input
          id="displayName"
          class="form-input"
          formControlName="displayName"
          autocomplete="name"
        />
        <csh-field-errors [messages]="submission.messagesFor('displayName')" />
      </div>

      <div>
        <label class="form-label" for="email">Work email</label>
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
          autocomplete="new-password"
        />
        <p class="mt-1 text-xs text-slate-500">At least 8 characters, with a letter and a digit.</p>
        <csh-field-errors [messages]="submission.messagesFor('password')" />
      </div>

      <div>
        <label class="form-label" for="confirmPassword">Confirm password</label>
        <input
          id="confirmPassword"
          class="form-input"
          type="password"
          formControlName="confirmPassword"
          autocomplete="new-password"
        />
        <csh-field-errors [messages]="submission.messagesFor('confirmPassword')" />
      </div>

      <button class="button-primary" type="submit" [disabled]="submission.pending()">
        Create my company
      </button>

      <p class="text-center text-sm text-slate-600">
        Already have an account?
        <a routerLink="/login" class="font-medium text-teal-700">Log in</a>
      </p>
    </form>
  `,
})
export class SignUpPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    companyName: ['', Validators.required],
    headcount: inject(FormBuilder).control<number | null>(null, [
      Validators.required,
      Validators.min(1),
    ]),
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    confirmPassword: ['', Validators.required],
  });
  protected readonly submission = new FormSubmission(this.form, {
    companyName: 'Enter the company name.',
    headcount: 'Enter the number of employees, at least 1.',
    displayName: 'Enter your name.',
    email: 'Enter a valid email address.',
    password: 'Choose a password.',
    confirmPassword: 'Confirm your password.',
  });

  protected signUp(): void {
    this.submission.submit(
      () => {
        const value = this.form.getRawValue();
        return this.auth.signUp({ ...value, headcount: value.headcount! });
      },
      () => void this.router.navigateByUrl('/'),
    );
  }
}
