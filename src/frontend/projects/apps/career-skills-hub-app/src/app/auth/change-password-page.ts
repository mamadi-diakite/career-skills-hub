import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService, FormSubmission } from '@career-skills-hub/shared/core';
import { FieldErrors } from '@career-skills-hub/shared/ui';

@Component({
  selector: 'csh-change-password-page',
  imports: [ReactiveFormsModule, FieldErrors],
  template: `
    <form class="form-card" [formGroup]="form" (ngSubmit)="change()" novalidate>
      <h1 class="text-2xl font-bold tracking-tight">Change password</h1>

      @if (changed()) {
        <p class="alert-info" role="status">Your password has been changed.</p>
      }
      @if (submission.errors().form; as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      <div>
        <label class="form-label" for="currentPassword">Current password</label>
        <input
          id="currentPassword"
          class="form-input"
          type="password"
          formControlName="currentPassword"
          autocomplete="current-password"
        />
        <csh-field-errors [messages]="submission.messagesFor('currentPassword')" />
      </div>

      <div>
        <label class="form-label" for="newPassword">New password</label>
        <input
          id="newPassword"
          class="form-input"
          type="password"
          formControlName="newPassword"
          autocomplete="new-password"
        />
        <p class="mt-1 text-xs text-slate-500">At least 8 characters, with a letter and a digit.</p>
        <csh-field-errors [messages]="submission.messagesFor('newPassword')" />
      </div>

      <div>
        <label class="form-label" for="confirmPassword">Confirm new password</label>
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
        Change password
      </button>
    </form>
  `,
})
export class ChangePasswordPage {
  private readonly auth = inject(AuthService);

  protected readonly changed = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    currentPassword: ['', Validators.required],
    newPassword: ['', Validators.required],
    confirmPassword: ['', Validators.required],
  });
  protected readonly submission = new FormSubmission(this.form, {
    currentPassword: 'Enter your current password.',
    newPassword: 'Choose a new password.',
    confirmPassword: 'Confirm your new password.',
  });

  protected change(): void {
    this.changed.set(false);
    this.submission.submit(
      () => this.auth.changePassword(this.form.getRawValue()),
      () => {
        this.form.reset();
        this.changed.set(true);
      },
    );
  }
}
