import { Component, computed, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  EmployeesService,
  FormSubmission,
  OrganisationService,
  reloadable,
  SessionStore,
  valueOr,
} from '@career-skills-hub/shared/core';
import { FieldErrors } from '@career-skills-hub/shared/ui';

/** The employees the user may see: HR everyone, a manager the team, an employee themselves. */
@Component({
  selector: 'csh-employees-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <section class="space-y-6">
      <div class="space-y-2">
        <h1 class="text-2xl font-bold tracking-tight">Employees</h1>
        <p class="text-slate-600">{{ intro }}</p>
      </div>

      @if (canEdit) {
        <form class="panel" [formGroup]="form" (ngSubmit)="create()" novalidate>
          <h2 class="text-lg font-semibold">Add an employee</h2>
          @if (submission.errors().form; as message) {
            <p class="alert-error" role="alert">{{ message }}</p>
          }
          <div class="grid gap-4 sm:grid-cols-2">
            <div>
              <label class="form-label" for="firstName">First name</label>
              <input id="firstName" class="form-input" formControlName="firstName" />
              <csh-field-errors [messages]="submission.messagesFor('firstName')" />
            </div>
            <div>
              <label class="form-label" for="lastName">Last name</label>
              <input id="lastName" class="form-input" formControlName="lastName" />
              <csh-field-errors [messages]="submission.messagesFor('lastName')" />
            </div>
            <div>
              <label class="form-label" for="email">Work email</label>
              <input id="email" class="form-input" type="email" formControlName="email" />
              <csh-field-errors [messages]="submission.messagesFor('email')" />
            </div>
            <div>
              <label class="form-label" for="hireDate">Hire date</label>
              <input id="hireDate" class="form-input" type="date" formControlName="hireDate" />
              <csh-field-errors [messages]="submission.messagesFor('hireDate')" />
            </div>
            <div>
              <label class="form-label" for="jobId">Job</label>
              <select id="jobId" class="form-input" formControlName="jobId">
                <option value="">Choose a job</option>
                @for (job of jobs(); track job.id) {
                  <option [value]="job.id">{{ job.title }}</option>
                }
              </select>
              <csh-field-errors [messages]="submission.messagesFor('jobId')" />
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
            <div>
              <label class="form-label" for="managerId">Manager (optional)</label>
              <select id="managerId" class="form-input" formControlName="managerId">
                <option value="">No manager</option>
                @for (employee of employeeList(); track employee.id) {
                  <option [value]="employee.id">
                    {{ employee.firstName }} {{ employee.lastName }}
                  </option>
                }
              </select>
              <csh-field-errors [messages]="submission.messagesFor('managerId')" />
            </div>
          </div>
          <button class="button-primary sm:w-auto" type="submit" [disabled]="submission.pending()">
            Add the employee
          </button>
        </form>
      }

      @let current = employees.state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the employees…</p>
      } @else if (current.status === 'ready') {
        <ul class="divide-y divide-slate-200 rounded-xl border border-slate-200 bg-white">
          @for (employee of current.value; track employee.id) {
            <li class="flex items-center justify-between gap-4 px-4 py-3" data-testid="employee">
              <a class="link font-medium" [routerLink]="['/employees', employee.id]">
                {{ employee.firstName }} {{ employee.lastName }}
              </a>
              <span class="text-sm text-slate-600">
                {{ employee.jobTitle }} · {{ employee.departmentName }}
              </span>
            </li>
          } @empty {
            <li class="px-4 py-3 text-slate-600">No employees yet.</li>
          }
        </ul>
      } @else {
        <p class="alert-error" role="alert">The employees could not be loaded.</p>
      }
    </section>
  `,
})
export class EmployeesPage {
  private readonly employeesService = inject(EmployeesService);
  private readonly organisation = inject(OrganisationService);
  private readonly router = inject(Router);
  private readonly role = inject(SessionStore).user()?.role;

  protected readonly canEdit = this.role === 'HrAdmin';
  protected readonly intro =
    this.role === 'HrAdmin'
      ? 'Everyone in the company.'
      : this.role === 'Manager'
        ? 'You and your team.'
        : 'Your own record.';
  protected readonly employees = reloadable(() => this.employeesService.list());
  protected readonly employeeList = computed(() => valueOr(this.employees.state(), []));
  private readonly jobList = reloadable(() => this.organisation.jobs());
  protected readonly jobs = computed(() => valueOr(this.jobList.state(), []));
  private readonly departmentList = reloadable(() => this.organisation.departments());
  protected readonly departments = computed(() => valueOr(this.departmentList.state(), []));

  protected readonly form = inject(NonNullableFormBuilder).group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    hireDate: ['', Validators.required],
    jobId: ['', Validators.required],
    departmentId: ['', Validators.required],
    managerId: [''],
  });
  protected readonly submission = new FormSubmission(this.form, {
    firstName: 'Enter the first name.',
    lastName: 'Enter the last name.',
    email: 'Enter a valid email address.',
    hireDate: 'Enter the hire date.',
    jobId: 'Choose a job.',
    departmentId: 'Choose a department.',
  });

  protected create(): void {
    const value = this.form.getRawValue();
    this.submission.submit(
      () => this.employeesService.create({ ...value, managerId: value.managerId || null }),
      (details) => void this.router.navigate(['/employees', details.employee.id]),
    );
  }
}
