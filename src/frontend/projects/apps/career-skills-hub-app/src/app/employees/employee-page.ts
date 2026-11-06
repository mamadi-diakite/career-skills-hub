import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  EmployeesService,
  FormSubmission,
  Invitation,
  OrganisationService,
  reloadable,
  roleLabel,
  SessionStore,
  USER_ROLES,
  UserRole,
  valueOr,
} from '@career-skills-hub/shared/core';
import { FieldErrors } from '@career-skills-hub/shared/ui';

/** One employee and their job history; HR admins record job changes and invite the employee. */
@Component({
  selector: 'csh-employee-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <section class="space-y-6">
      <a class="link text-sm" routerLink="/employees">← All employees</a>

      @if (notice(); as message) {
        <p class="alert-info" role="status">{{ message }}</p>
      }
      @if (invitation(); as sent) {
        <div class="alert-info" role="status" data-testid="invitation">
          <p>
            An account has been created for {{ sent.email }} as {{ roleLabel(sent.role) }}. Give
            them this temporary password; it is shown only once and must be changed at the first
            login:
          </p>
          <p class="mt-2 font-mono text-base font-semibold" data-testid="temporary-password">
            {{ sent.temporaryPassword }}
          </p>
        </div>
      }

      @let current = details.state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the employee…</p>
      } @else if (current.status === 'ready') {
        @let employee = current.value.employee;
        <h1 class="text-2xl font-bold tracking-tight">
          {{ employee.firstName }} {{ employee.lastName }}
        </h1>

        <dl class="panel grid gap-4 sm:grid-cols-2" data-testid="employee-details">
          <div>
            <dt class="text-sm text-slate-600">Work email</dt>
            <dd>{{ employee.email }}</dd>
          </div>
          <div>
            <dt class="text-sm text-slate-600">Hire date</dt>
            <dd>{{ employee.hireDate }}</dd>
          </div>
          <div>
            <dt class="text-sm text-slate-600">Job</dt>
            <dd>{{ employee.jobTitle }}</dd>
          </div>
          <div>
            <dt class="text-sm text-slate-600">Department</dt>
            <dd>{{ employee.departmentName }}</dd>
          </div>
          <div>
            <dt class="text-sm text-slate-600">Manager</dt>
            <dd>{{ employee.managerName ?? 'No manager' }}</dd>
          </div>
          <div>
            <dt class="text-sm text-slate-600">Account</dt>
            <dd>
              {{ employee.accountRole ? roleLabel(employee.accountRole) : 'Not invited yet' }}
            </dd>
          </div>
        </dl>

        <section class="panel">
          <h2 class="text-lg font-semibold">Job history</h2>
          <table class="w-full text-left text-sm">
            <thead>
              <tr class="text-slate-600">
                <th class="py-2 font-medium">Job</th>
                <th class="py-2 font-medium">Department</th>
                <th class="py-2 font-medium">From</th>
                <th class="py-2 font-medium">To</th>
              </tr>
            </thead>
            <tbody>
              @for (assignment of current.value.jobHistory; track assignment.startDate) {
                <tr class="border-t border-slate-200" data-testid="assignment">
                  <td class="py-2">{{ assignment.jobTitle }}</td>
                  <td class="py-2">{{ assignment.departmentName }}</td>
                  <td class="py-2">{{ assignment.startDate }}</td>
                  <td class="py-2">{{ assignment.endDate ?? 'Today' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </section>

        @if (canEdit) {
          <form
            class="panel"
            [formGroup]="jobChangeForm"
            (ngSubmit)="changeJob()"
            novalidate
            data-testid="job-change-form"
          >
            <h2 class="text-lg font-semibold">Record a job change</h2>
            @if (jobChangeSubmission.errors().form; as message) {
              <p class="alert-error" role="alert">{{ message }}</p>
            }
            <div class="grid gap-4 sm:grid-cols-3">
              <div>
                <label class="form-label" for="jobId">New job</label>
                <select id="jobId" class="form-input" formControlName="jobId">
                  <option value="">Choose a job</option>
                  @for (job of jobs(); track job.id) {
                    <option [value]="job.id">{{ job.title }}</option>
                  }
                </select>
                <csh-field-errors [messages]="jobChangeSubmission.messagesFor('jobId')" />
              </div>
              <div>
                <label class="form-label" for="departmentId">Department</label>
                <select id="departmentId" class="form-input" formControlName="departmentId">
                  <option value="">Choose a department</option>
                  @for (department of departments(); track department.id) {
                    <option [value]="department.id">{{ department.name }}</option>
                  }
                </select>
                <csh-field-errors [messages]="jobChangeSubmission.messagesFor('departmentId')" />
              </div>
              <div>
                <label class="form-label" for="effectiveDate">Effective date</label>
                <input
                  id="effectiveDate"
                  class="form-input"
                  type="date"
                  formControlName="effectiveDate"
                />
                <csh-field-errors [messages]="jobChangeSubmission.messagesFor('effectiveDate')" />
              </div>
            </div>
            <button
              class="button-primary sm:w-auto"
              type="submit"
              [disabled]="jobChangeSubmission.pending()"
            >
              Record the job change
            </button>
          </form>

          @if (!employee.accountRole) {
            <form
              class="panel"
              [formGroup]="invitationForm"
              (ngSubmit)="invite()"
              novalidate
              data-testid="invitation-form"
            >
              <h2 class="text-lg font-semibold">Invite to Career Skills Hub</h2>
              <p class="text-sm text-slate-600">
                Creates an account with a temporary password for {{ employee.email }}.
              </p>
              @if (invitationSubmission.errors().form; as message) {
                <p class="alert-error" role="alert">{{ message }}</p>
              }
              <div>
                <label class="form-label" for="role">Role</label>
                <select id="role" class="form-input" formControlName="role">
                  @for (option of roles; track option.value) {
                    <option [value]="option.value">{{ option.label }}</option>
                  }
                </select>
                <csh-field-errors [messages]="invitationSubmission.messagesFor('role')" />
              </div>
              <button
                class="button-primary sm:w-auto"
                type="submit"
                [disabled]="invitationSubmission.pending()"
              >
                Create the account
              </button>
            </form>
          }
        }
      } @else if (current.status === 'forbidden') {
        <p class="alert-error" role="alert">You can only see your own record and your team.</p>
      } @else if (current.status === 'missing') {
        <p class="alert-error" role="alert">This employee does not exist.</p>
      } @else {
        <p class="alert-error" role="alert">The employee could not be loaded.</p>
      }
    </section>
  `,
})
export class EmployeePage {
  private readonly employees = inject(EmployeesService);
  private readonly organisation = inject(OrganisationService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id')!;

  protected readonly roleLabel = roleLabel;
  protected readonly roles = USER_ROLES;
  protected readonly canEdit = inject(SessionStore).user()?.role === 'HrAdmin';
  protected readonly notice = signal<string | null>(null);
  protected readonly invitation = signal<Invitation | null>(null);

  protected readonly details = reloadable(() => this.employees.get(this.id));
  private readonly jobList = reloadable(() => this.organisation.jobs());
  protected readonly jobs = computed(() => valueOr(this.jobList.state(), []));
  private readonly departmentList = reloadable(() => this.organisation.departments());
  protected readonly departments = computed(() => valueOr(this.departmentList.state(), []));

  protected readonly jobChangeForm = this.formBuilder.group({
    jobId: ['', Validators.required],
    departmentId: ['', Validators.required],
    effectiveDate: ['', Validators.required],
  });
  protected readonly jobChangeSubmission = new FormSubmission(this.jobChangeForm, {
    jobId: 'Choose the new job.',
    departmentId: 'Choose a department.',
    effectiveDate: 'Enter the date of the change.',
  });

  protected readonly invitationForm = this.formBuilder.group({
    role: ['Employee' as UserRole, Validators.required],
  });
  protected readonly invitationSubmission = new FormSubmission(this.invitationForm, {
    role: 'Choose a role.',
  });

  protected changeJob(): void {
    this.notice.set(null);
    const request = this.jobChangeForm.getRawValue();
    this.jobChangeSubmission.submit(
      () => this.employees.changeJob(this.id, request),
      () => {
        this.jobChangeForm.reset();
        this.notice.set('The job change has been recorded.');
        this.details.reload();
      },
    );
  }

  protected invite(): void {
    this.notice.set(null);
    const { role } = this.invitationForm.getRawValue();
    this.invitationSubmission.submit(
      () => this.employees.invite(this.id, role),
      (invitation) => {
        this.invitation.set(invitation);
        this.details.reload();
      },
    );
  }
}
