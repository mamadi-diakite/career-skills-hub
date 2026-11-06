import { Component, input } from '@angular/core';

/** Error messages shown under a form field. */
@Component({
  selector: 'csh-field-errors',
  template: `
    @for (message of messages(); track message) {
      <p class="mt-1 text-sm text-rose-600">{{ message }}</p>
    }
  `,
})
export class FieldErrors {
  readonly messages = input<readonly string[]>([]);
}
