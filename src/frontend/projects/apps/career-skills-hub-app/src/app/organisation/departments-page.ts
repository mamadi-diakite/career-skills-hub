import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  Department,
  FormSubmission,
  OrganisationService,
  reloadable,
  SessionStore,
  toFormErrors,
} from '@career-skills-hub/shared/core';
import { FieldErrors } from '@career-skills-hub/shared/ui';

/** Everyone sees the departments; HR admins add, rename and delete them. */
@Component({
  selector: 'csh-departments-page',
  imports: [ReactiveFormsModule, FieldErrors],
  template: `
    <section class="space-y-6">
      <div class="space-y-2">
        <h1 class="text-2xl font-bold tracking-tight">Departments</h1>
        <p class="text-slate-600">The teams of the company. Each job belongs to one of them.</p>
      </div>

      @if (actionError(); as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      @if (canEdit) {
        <form class="panel" [formGroup]="form" (ngSubmit)="save()" novalidate>
          <h2 class="text-lg font-semibold">
            {{ editing() ? 'Rename ' + editing()!.name : 'Add a department' }}
          </h2>
          @if (submission.errors().form; as message) {
            <p class="alert-error" role="alert">{{ message }}</p>
          }
          <div>
            <label class="form-label" for="name">Name</label>
            <input id="name" class="form-input" formControlName="name" />
            <csh-field-errors [messages]="submission.messagesFor('name')" />
          </div>
          <div class="flex gap-3">
            <button
              class="button-primary sm:w-auto"
              type="submit"
              [disabled]="submission.pending()"
            >
              {{ editing() ? 'Save' : 'Add' }}
            </button>
            @if (editing()) {
              <button class="button-secondary" type="button" (click)="cancel()">Cancel</button>
            }
          </div>
        </form>
      }

      @let current = departments.state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the departments…</p>
      } @else if (current.status === 'ready') {
        <ul class="divide-y divide-slate-200 rounded-xl border border-slate-200 bg-white">
          @for (department of current.value; track department.id) {
            <li class="flex items-center justify-between gap-4 px-4 py-3" data-testid="department">
              <span class="font-medium">{{ department.name }}</span>
              @if (canEdit) {
                <span class="flex gap-3 text-sm">
                  <button type="button" class="link" (click)="edit(department)">Rename</button>
                  <button type="button" class="link" (click)="remove(department)">Delete</button>
                </span>
              }
            </li>
          } @empty {
            <li class="px-4 py-3 text-slate-600">No departments yet.</li>
          }
        </ul>
      } @else {
        <p class="alert-error" role="alert">The departments could not be loaded.</p>
      }
    </section>
  `,
})
export class DepartmentsPage {
  private readonly organisation = inject(OrganisationService);

  protected readonly canEdit = inject(SessionStore).user()?.role === 'HrAdmin';
  protected readonly departments = reloadable(() => this.organisation.departments());
  protected readonly editing = signal<Department | null>(null);
  protected readonly actionError = signal<string | null>(null);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', Validators.required],
  });
  protected readonly submission = new FormSubmission(this.form, {
    name: 'Enter the name of the department.',
  });

  protected edit(department: Department): void {
    this.editing.set(department);
    this.form.setValue({ name: department.name });
  }

  protected cancel(): void {
    this.editing.set(null);
    this.form.reset();
  }

  protected save(): void {
    this.actionError.set(null);
    const editing = this.editing();
    const request = this.form.getRawValue();
    this.submission.submit(
      () =>
        editing
          ? this.organisation.updateDepartment(editing.id, request)
          : this.organisation.createDepartment(request),
      () => {
        this.cancel();
        this.departments.reload();
      },
    );
  }

  protected remove(department: Department): void {
    this.actionError.set(null);
    this.organisation.deleteDepartment(department.id).subscribe({
      next: () => this.departments.reload(),
      error: (error: unknown) => this.actionError.set(toFormErrors(error).form),
    });
  }
}
