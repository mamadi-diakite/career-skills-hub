import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { finalize, Observable } from 'rxjs';

export interface FormErrors {
  /** Messages by form field name. */
  readonly fields: Readonly<Record<string, readonly string[]>>;
  /** Message about the whole form, e.g. wrong credentials. */
  readonly form: string | null;
}

export const NO_FORM_ERRORS: FormErrors = { fields: {}, form: null };

const unexpectedError = 'Something went wrong. Please try again.';

/** Reads the problem details the API returns: field errors for 400, the title for other 4xx. */
export function toFormErrors(error: unknown): FormErrors {
  if (error instanceof HttpErrorResponse && error.status >= 400 && error.status < 500) {
    const body = error.error as { title?: string; errors?: Record<string, string[]> } | null;
    if (body?.errors) {
      return {
        fields: Object.fromEntries(
          Object.entries(body.errors).map(([field, messages]) => [camelCase(field), messages]),
        ),
        form: null,
      };
    }

    return { fields: {}, form: body?.title ?? unexpectedError };
  }

  return { fields: {}, form: unexpectedError };
}

function camelCase(field: string): string {
  return field.charAt(0).toLowerCase() + field.slice(1);
}

/** Sends a reactive form to the API and keeps track of the pending state and the errors to show. */
export class FormSubmission {
  readonly pending = signal(false);
  readonly errors = signal<FormErrors>(NO_FORM_ERRORS);

  constructor(
    private readonly form: FormGroup,
    private readonly invalidMessages: Readonly<Record<string, string>>,
  ) {}

  /**
   * Server messages for a field, or its client-side message once the user has left it invalid.
   * `apiField` is the name the API uses when it differs, e.g. `levels.beginner`.
   */
  messagesFor(field: string, apiField = field): readonly string[] {
    const server = this.errors().fields[apiField];
    if (server) {
      return server;
    }

    const control = this.form.controls[field];
    return control.touched && control.invalid ? [this.invalidMessages[field]] : [];
  }

  submit<T>(request: () => Observable<T>, onSuccess: (value: T) => void): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.pending.set(true);
    this.errors.set(NO_FORM_ERRORS);
    request()
      .pipe(finalize(() => this.pending.set(false)))
      .subscribe({
        next: onSuccess,
        error: (error: unknown) => this.errors.set(toFormErrors(error)),
      });
  }
}
