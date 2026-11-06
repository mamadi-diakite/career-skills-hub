import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  CRITICALITIES,
  Criticality,
  FormSubmission,
  Job,
  OrganisationService,
  reloadable,
  RequiredSkill,
  SessionStore,
  SKILL_LEVELS,
  SkillLevel,
  toFormErrors,
  valueOr,
} from '@career-skills-hub/shared/core';
import { FieldErrors } from '@career-skills-hub/shared/ui';
import { tap } from 'rxjs';

/** One job and its required skills; HR admins edit, delete and set the required skills. */
@Component({
  selector: 'csh-job-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <section class="space-y-6">
      <a class="link text-sm" routerLink="/jobs">← All jobs</a>

      @if (notice(); as message) {
        <p class="alert-info" role="status">{{ message }}</p>
      }
      @if (actionError(); as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      @let current = job.state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the job…</p>
      } @else if (current.status === 'ready') {
        <div class="space-y-2">
          <h1 class="text-2xl font-bold tracking-tight">{{ current.value.title }}</h1>
          <p class="text-slate-600" data-testid="department">{{ current.value.departmentName }}</p>
          @if (current.value.description) {
            <p data-testid="description">{{ current.value.description }}</p>
          }
        </div>

        <section class="panel">
          <h2 class="text-lg font-semibold">Required skills</h2>
          <table class="w-full text-left text-sm">
            <thead>
              <tr class="text-slate-600">
                <th class="py-2 font-medium">Skill</th>
                <th class="py-2 font-medium">Expected level</th>
                <th class="py-2 font-medium">Criticality</th>
              </tr>
            </thead>
            <tbody>
              @for (required of current.value.requiredSkills; track required.skillId) {
                <tr class="border-t border-slate-200" data-testid="required-skill">
                  <td class="py-2">{{ required.skillName }}</td>
                  <td class="py-2">{{ required.expectedLevel }}</td>
                  <td class="py-2">{{ required.criticality }}</td>
                </tr>
              } @empty {
                <tr class="border-t border-slate-200">
                  <td class="py-2 text-slate-600" colspan="3">No required skills yet.</td>
                </tr>
              }
            </tbody>
          </table>
        </section>

        @if (canEdit) {
          <form
            class="panel"
            [formGroup]="requiredForm"
            (ngSubmit)="saveRequiredSkills()"
            novalidate
            data-testid="required-skills-form"
          >
            <h2 class="text-lg font-semibold">Set the required skills</h2>
            @if (requiredSubmission.errors().form; as message) {
              <p class="alert-error" role="alert">{{ message }}</p>
            }
            @for (message of requiredServerErrors(); track $index) {
              <p class="alert-error" role="alert">{{ message }}</p>
            }
            <csh-field-errors [messages]="requiredSubmission.messagesFor('rows')" />
            <div formArrayName="rows" class="space-y-3">
              @for (row of requiredForm.controls.rows.controls; track row; let i = $index) {
                <div
                  class="grid gap-3 sm:grid-cols-[2fr_1fr_1fr_auto]"
                  [formGroupName]="i"
                  data-testid="required-row"
                >
                  <select
                    class="form-input"
                    formControlName="skillId"
                    [attr.aria-label]="'Skill ' + (i + 1)"
                  >
                    <option value="">Choose a skill</option>
                    @for (domain of domains(); track domain.id) {
                      <optgroup [label]="domain.name">
                        @for (skill of domain.skills; track skill.id) {
                          <option [value]="skill.id">{{ skill.name }}</option>
                        }
                      </optgroup>
                    }
                  </select>
                  <select
                    class="form-input"
                    formControlName="expectedLevel"
                    [attr.aria-label]="'Expected level ' + (i + 1)"
                  >
                    @for (level of levels; track level) {
                      <option [value]="level">{{ level }}</option>
                    }
                  </select>
                  <select
                    class="form-input"
                    formControlName="criticality"
                    [attr.aria-label]="'Criticality ' + (i + 1)"
                  >
                    @for (criticality of criticalities; track criticality) {
                      <option [value]="criticality">{{ criticality }}</option>
                    }
                  </select>
                  <button class="link text-sm" type="button" (click)="removeRow(i)">Remove</button>
                </div>
              }
            </div>
            <div class="flex gap-3">
              <button class="button-secondary" type="button" (click)="addRow()">Add a skill</button>
              <button
                class="button-primary sm:w-auto"
                type="submit"
                [disabled]="requiredSubmission.pending()"
              >
                Save the required skills
              </button>
            </div>
          </form>

          <form
            class="panel"
            [formGroup]="jobForm"
            (ngSubmit)="saveJob()"
            novalidate
            data-testid="job-form"
          >
            <h2 class="text-lg font-semibold">Edit the job</h2>
            @if (jobSubmission.errors().form; as message) {
              <p class="alert-error" role="alert">{{ message }}</p>
            }
            <div class="grid gap-4 sm:grid-cols-2">
              <div>
                <label class="form-label" for="title">Title</label>
                <input id="title" class="form-input" formControlName="title" />
                <csh-field-errors [messages]="jobSubmission.messagesFor('title')" />
              </div>
              <div>
                <label class="form-label" for="departmentId">Department</label>
                <select id="departmentId" class="form-input" formControlName="departmentId">
                  @for (department of departments(); track department.id) {
                    <option [value]="department.id">{{ department.name }}</option>
                  }
                </select>
                <csh-field-errors [messages]="jobSubmission.messagesFor('departmentId')" />
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
              <csh-field-errors [messages]="jobSubmission.messagesFor('description')" />
            </div>
            <div class="flex gap-3">
              <button
                class="button-primary sm:w-auto"
                type="submit"
                [disabled]="jobSubmission.pending()"
              >
                Save the job
              </button>
              <button class="button-secondary" type="button" (click)="deleteJob()">
                Delete the job
              </button>
            </div>
          </form>
        }
      } @else if (current.status === 'missing') {
        <p class="alert-error" role="alert">This job does not exist.</p>
      } @else {
        <p class="alert-error" role="alert">The job could not be loaded.</p>
      }
    </section>
  `,
})
export class JobPage {
  private readonly organisation = inject(OrganisationService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id')!;

  protected readonly levels = SKILL_LEVELS;
  protected readonly criticalities = CRITICALITIES;
  protected readonly canEdit = inject(SessionStore).user()?.role === 'HrAdmin';
  protected readonly notice = signal<string | null>(null);
  protected readonly actionError = signal<string | null>(null);

  protected readonly jobForm = this.formBuilder.group({
    title: ['', Validators.required],
    departmentId: ['', Validators.required],
    description: [''],
  });
  protected readonly jobSubmission = new FormSubmission(this.jobForm, {
    title: 'Enter the title of the job.',
    departmentId: 'Choose a department.',
  });

  protected readonly requiredForm = this.formBuilder.group({
    rows: this.formBuilder.array<ReturnType<JobPage['row']>>([]),
  });
  protected readonly requiredSubmission = new FormSubmission(this.requiredForm, {
    rows: 'Choose a skill on every line.',
  });
  protected readonly requiredServerErrors = computed(() =>
    Object.values(this.requiredSubmission.errors().fields).flat(),
  );

  protected readonly job = reloadable(() =>
    this.organisation.job(this.id).pipe(tap((job) => this.fill(job))),
  );
  private readonly departmentList = reloadable(() => this.organisation.departments());
  protected readonly departments = computed(() => valueOr(this.departmentList.state(), []));
  private readonly framework = reloadable(() => this.organisation.skillDomains());
  protected readonly domains = computed(() => valueOr(this.framework.state(), []));

  protected addRow(): void {
    this.requiredForm.controls.rows.push(this.row());
  }

  protected removeRow(index: number): void {
    this.requiredForm.controls.rows.removeAt(index);
  }

  protected saveRequiredSkills(): void {
    this.clearMessages();
    const items = this.requiredForm.controls.rows.getRawValue();
    this.requiredSubmission.submit(
      () => this.organisation.setRequiredSkills(this.id, items),
      () => this.saved('The required skills have been saved.'),
    );
  }

  protected saveJob(): void {
    this.clearMessages();
    const value = this.jobForm.getRawValue();
    this.jobSubmission.submit(
      () =>
        this.organisation.updateJob(this.id, {
          title: value.title,
          departmentId: value.departmentId,
          description: value.description || null,
        }),
      () => this.saved('The job has been saved.'),
    );
  }

  protected deleteJob(): void {
    this.clearMessages();
    this.organisation.deleteJob(this.id).subscribe({
      next: () => void this.router.navigate(['/jobs']),
      error: (error: unknown) => this.actionError.set(toFormErrors(error).form),
    });
  }

  private fill(job: Job): void {
    this.jobForm.setValue({
      title: job.title,
      departmentId: job.departmentId,
      description: job.description ?? '',
    });
    const rows = this.requiredForm.controls.rows;
    rows.clear();
    job.requiredSkills.forEach((required) => rows.push(this.row(required)));
  }

  private row(required?: RequiredSkill) {
    return this.formBuilder.group({
      skillId: [required?.skillId ?? '', Validators.required],
      expectedLevel: [required?.expectedLevel ?? ('Intermediate' as SkillLevel)],
      criticality: [required?.criticality ?? ('Medium' as Criticality)],
    });
  }

  private saved(message: string): void {
    this.notice.set(message);
    this.job.reload();
  }

  private clearMessages(): void {
    this.notice.set(null);
    this.actionError.set(null);
  }
}
