import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CompanyService, FormSubmission, SessionStore } from '@career-skills-hub/shared/core';
import { FieldErrors } from '@career-skills-hub/shared/ui';

/** Everyone reads the company profile; only HR admins can change it. */
@Component({
  selector: 'csh-company-page',
  imports: [ReactiveFormsModule, FieldErrors],
  template: `
    <form class="form-card" [formGroup]="form" (ngSubmit)="save()" novalidate>
      <h1 class="text-2xl font-bold tracking-tight">Company profile</h1>

      @if (loadFailed()) {
        <p class="alert-error" role="alert">The company profile could not be loaded.</p>
      }
      @if (saved()) {
        <p class="alert-info" role="status">The company profile has been saved.</p>
      }
      @if (submission.errors().form; as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      <div>
        <label class="form-label" for="name">Company name</label>
        <input id="name" class="form-input" formControlName="name" autocomplete="organization" />
        <csh-field-errors [messages]="submission.messagesFor('name')" />
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

      @if (canEdit) {
        <button class="button-primary" type="submit" [disabled]="submission.pending()">Save</button>
      } @else {
        <p class="text-sm text-slate-600">Only HR admins can change the company profile.</p>
      }
    </form>
  `,
})
export class CompanyPage {
  private readonly companies = inject(CompanyService);

  protected readonly canEdit = inject(SessionStore).user()?.role === 'HrAdmin';
  protected readonly loadFailed = signal(false);
  protected readonly saved = signal(false);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    name: ['', Validators.required],
    headcount: inject(FormBuilder).control<number | null>(null, [
      Validators.required,
      Validators.min(1),
    ]),
  });
  protected readonly submission = new FormSubmission(this.form, {
    name: 'Enter the company name.',
    headcount: 'Enter the number of employees, at least 1.',
  });

  constructor() {
    if (!this.canEdit) {
      this.form.disable();
    }

    this.companies.get().subscribe({
      next: (company) => this.form.patchValue(company),
      error: () => this.loadFailed.set(true),
    });
  }

  protected save(): void {
    this.saved.set(false);
    this.submission.submit(
      () => {
        const value = this.form.getRawValue();
        return this.companies.update({ name: value.name, headcount: value.headcount! });
      },
      (company) => {
        this.form.patchValue(company);
        this.saved.set(true);
      },
    );
  }
}
