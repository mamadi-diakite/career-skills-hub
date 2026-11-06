import { Component, computed, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  FormSubmission,
  OrganisationService,
  reloadable,
  SessionStore,
  valueOr,
} from '@career-skills-hub/shared/core';
import { FieldErrors } from '@career-skills-hub/shared/ui';

/** The jobs of the company; HR admins add new ones and open them to set required skills. */
@Component({
  selector: 'csh-jobs-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <section class="space-y-6">
      <div class="space-y-2">
        <h1 class="text-2xl font-bold tracking-tight">Jobs</h1>
        <p class="text-slate-600">Each job lists the skills it requires and the level expected.</p>
      </div>

      @if (canEdit) {
        <form class="panel" [formGroup]="form" (ngSubmit)="create()" novalidate>
          <h2 class="text-lg font-semibold">Add a job</h2>
          @if (submission.errors().form; as message) {
            <p class="alert-error" role="alert">{{ message }}</p>
          }
          <div class="grid gap-4 sm:grid-cols-2">
            <div>
              <label class="form-label" for="title">Title</label>
              <input id="title" class="form-input" formControlName="title" />
              <csh-field-errors [messages]="submission.messagesFor('title')" />
            </div>
            <div>
              <label class="form-label" for="departmentId">Department</label>
              <select id="departmentId" class="form-input" formControlName="departmentId">
                <option value="">Choose a department</option>
                @for (department of departments(); track department.id) {
                  <option [value]="department.id">{{ department.name }}</option>
                }
              </select>
              <csh-field-errors [messages]="submission.messagesFor('departmentId')" />
            </div>
          </div>
          <div>
            <label class="form-label" for="description">Description (optional)</label>
            <textarea
              id="description"
              class="form-input"
              rows="3"
              formControlName="description"
            ></textarea>
            <csh-field-errors [messages]="submission.messagesFor('description')" />
          </div>
          <button class="button-primary sm:w-auto" type="submit" [disabled]="submission.pending()">
            Add the job
          </button>
        </form>
      }

      @let current = jobs.state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the jobs…</p>
      } @else if (current.status === 'ready') {
        <ul class="divide-y divide-slate-200 rounded-xl border border-slate-200 bg-white">
          @for (job of current.value; track job.id) {
            <li class="flex items-center justify-between gap-4 px-4 py-3" data-testid="job">
              <a class="link font-medium" [routerLink]="['/jobs', job.id]">{{ job.title }}</a>
              <span class="text-sm text-slate-600">
                {{ job.departmentName }} · {{ job.requiredSkills.length }} required skills
              </span>
            </li>
          } @empty {
            <li class="px-4 py-3 text-slate-600">No jobs yet.</li>
          }
        </ul>
      } @else {
        <p class="alert-error" role="alert">The jobs could not be loaded.</p>
      }
    </section>
  `,
})
export class JobsPage {
  private readonly organisation = inject(OrganisationService);
  private readonly router = inject(Router);

  protected readonly canEdit = inject(SessionStore).user()?.role === 'HrAdmin';
  protected readonly jobs = reloadable(() => this.organisation.jobs());
  private readonly departmentList = reloadable(() => this.organisation.departments());
  protected readonly departments = computed(() => valueOr(this.departmentList.state(), []));
  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', Validators.required],
    departmentId: ['', Validators.required],
    description: [''],
  });
  protected readonly submission = new FormSubmission(this.form, {
    title: 'Enter the title of the job.',
    departmentId: 'Choose a department.',
  });

  protected create(): void {
    const value = this.form.getRawValue();
    this.submission.submit(
      () =>
        this.organisation.createJob({
          title: value.title,
          departmentId: value.departmentId,
          description: value.description || null,
        }),
      (job) => void this.router.navigate(['/jobs', job.id]),
    );
  }
}
