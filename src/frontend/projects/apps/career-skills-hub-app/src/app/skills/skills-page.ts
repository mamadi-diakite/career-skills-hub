import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  FormSubmission,
  OrganisationService,
  reloadable,
  SessionStore,
  Skill,
  SkillDomain,
  toFormErrors,
  valueOr,
} from '@career-skills-hub/shared/core';
import { FieldErrors } from '@career-skills-hub/shared/ui';

const levelFields = [
  { key: 'beginner', label: 'Beginner' },
  { key: 'intermediate', label: 'Intermediate' },
  { key: 'advanced', label: 'Advanced' },
  { key: 'expert', label: 'Expert' },
] as const;

/** The skills framework: domains, their skills and what each of the four levels means. */
@Component({
  selector: 'csh-skills-page',
  imports: [ReactiveFormsModule, FieldErrors],
  template: `
    <section class="space-y-6">
      <div class="space-y-2">
        <h1 class="text-2xl font-bold tracking-tight">Skills framework</h1>
        <p class="text-slate-600">
          Skills are grouped by domain and assessed on four levels: Beginner, Intermediate, Advanced
          and Expert.
        </p>
      </div>

      @if (actionError(); as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      @if (canEdit) {
        <div class="grid gap-6 lg:grid-cols-2">
          <form
            class="panel"
            [formGroup]="domainForm"
            (ngSubmit)="saveDomain()"
            novalidate
            data-testid="domain-form"
          >
            <h2 class="text-lg font-semibold">
              {{ editingDomain() ? 'Edit ' + editingDomain()!.name : 'Add a skill domain' }}
            </h2>
            @if (domainSubmission.errors().form; as message) {
              <p class="alert-error" role="alert">{{ message }}</p>
            }
            <div>
              <label class="form-label" for="domainName">Name</label>
              <input id="domainName" class="form-input" formControlName="name" />
              <csh-field-errors [messages]="domainSubmission.messagesFor('name')" />
            </div>
            <div>
              <label class="form-label" for="domainDescription">Description (optional)</label>
              <input id="domainDescription" class="form-input" formControlName="description" />
              <csh-field-errors [messages]="domainSubmission.messagesFor('description')" />
            </div>
            <div class="flex gap-3">
              <button
                class="button-primary sm:w-auto"
                type="submit"
                [disabled]="domainSubmission.pending()"
              >
                {{ editingDomain() ? 'Save' : 'Add' }}
              </button>
              @if (editingDomain()) {
                <button class="button-secondary" type="button" (click)="cancelDomain()">
                  Cancel
                </button>
              }
            </div>
          </form>

          <form
            class="panel"
            [formGroup]="skillForm"
            (ngSubmit)="saveSkill()"
            novalidate
            data-testid="skill-form"
          >
            <h2 class="text-lg font-semibold">
              {{ editingSkill() ? 'Edit ' + editingSkill()!.name : 'Add a skill' }}
            </h2>
            @if (skillSubmission.errors().form; as message) {
              <p class="alert-error" role="alert">{{ message }}</p>
            }
            <div>
              <label class="form-label" for="skillDomainId">Domain</label>
              <select id="skillDomainId" class="form-input" formControlName="domainId">
                <option value="">Choose a domain</option>
                @for (domain of domains(); track domain.id) {
                  <option [value]="domain.id">{{ domain.name }}</option>
                }
              </select>
              <csh-field-errors [messages]="skillSubmission.messagesFor('domainId')" />
            </div>
            <div>
              <label class="form-label" for="skillName">Name</label>
              <input id="skillName" class="form-input" formControlName="name" />
              <csh-field-errors [messages]="skillSubmission.messagesFor('name')" />
            </div>
            <div>
              <label class="form-label" for="skillDescription">Description (optional)</label>
              <input id="skillDescription" class="form-input" formControlName="description" />
              <csh-field-errors [messages]="skillSubmission.messagesFor('description')" />
            </div>
            @for (level of levelFields; track level.key) {
              <div>
                <label class="form-label" [for]="level.key">{{ level.label }} level</label>
                <textarea
                  [id]="level.key"
                  class="form-input"
                  rows="2"
                  [formControlName]="level.key"
                ></textarea>
                <csh-field-errors
                  [messages]="skillSubmission.messagesFor(level.key, 'levels.' + level.key)"
                />
              </div>
            }
            <div class="flex gap-3">
              <button
                class="button-primary sm:w-auto"
                type="submit"
                [disabled]="skillSubmission.pending()"
              >
                {{ editingSkill() ? 'Save' : 'Add' }}
              </button>
              @if (editingSkill()) {
                <button class="button-secondary" type="button" (click)="cancelSkill()">
                  Cancel
                </button>
              }
            </div>
          </form>
        </div>
      }

      @let current = framework.state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the skills framework…</p>
      } @else if (current.status === 'ready') {
        @for (domain of current.value; track domain.id) {
          <section class="panel" data-testid="domain">
            <div class="flex items-start justify-between gap-4">
              <div>
                <h2 class="text-lg font-semibold">{{ domain.name }}</h2>
                @if (domain.description) {
                  <p class="text-sm text-slate-600">{{ domain.description }}</p>
                }
              </div>
              @if (canEdit) {
                <span class="flex gap-3 text-sm">
                  <button type="button" class="link" (click)="editDomain(domain)">Edit</button>
                  <button type="button" class="link" (click)="removeDomain(domain)">Delete</button>
                </span>
              }
            </div>
            <ul class="space-y-4">
              @for (skill of domain.skills; track skill.id) {
                <li class="rounded-lg border border-slate-200 p-4" data-testid="skill">
                  <div class="flex items-start justify-between gap-4">
                    <div>
                      <p class="font-medium">{{ skill.name }}</p>
                      @if (skill.description) {
                        <p class="text-sm text-slate-600">{{ skill.description }}</p>
                      }
                    </div>
                    @if (canEdit) {
                      <span class="flex gap-3 text-sm">
                        <button type="button" class="link" (click)="editSkill(skill)">Edit</button>
                        <button type="button" class="link" (click)="removeSkill(skill)">
                          Delete
                        </button>
                      </span>
                    }
                  </div>
                  <dl class="mt-3 grid gap-2 text-sm sm:grid-cols-2">
                    @for (level of levelFields; track level.key) {
                      <div>
                        <dt class="font-medium text-slate-700">{{ level.label }}</dt>
                        <dd class="text-slate-600">{{ skill.levels[level.key] }}</dd>
                      </div>
                    }
                  </dl>
                </li>
              } @empty {
                <li class="text-sm text-slate-600">No skills in this domain yet.</li>
              }
            </ul>
          </section>
        } @empty {
          <p class="text-slate-600">No skill domains yet.</p>
        }
      } @else {
        <p class="alert-error" role="alert">The skills framework could not be loaded.</p>
      }
    </section>
  `,
})
export class SkillsPage {
  private readonly organisation = inject(OrganisationService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly levelFields = levelFields;
  protected readonly canEdit = inject(SessionStore).user()?.role === 'HrAdmin';
  protected readonly framework = reloadable(() => this.organisation.skillDomains());
  protected readonly domains = computed(() => valueOr(this.framework.state(), []));
  protected readonly editingDomain = signal<SkillDomain | null>(null);
  protected readonly editingSkill = signal<Skill | null>(null);
  protected readonly actionError = signal<string | null>(null);

  protected readonly domainForm = this.formBuilder.group({
    name: ['', Validators.required],
    description: [''],
  });
  protected readonly domainSubmission = new FormSubmission(this.domainForm, {
    name: 'Enter the name of the domain.',
  });

  protected readonly skillForm = this.formBuilder.group({
    domainId: ['', Validators.required],
    name: ['', Validators.required],
    description: [''],
    beginner: ['', Validators.required],
    intermediate: ['', Validators.required],
    advanced: ['', Validators.required],
    expert: ['', Validators.required],
  });
  protected readonly skillSubmission = new FormSubmission(this.skillForm, {
    domainId: 'Choose a domain.',
    name: 'Enter the name of the skill.',
    beginner: 'Describe the Beginner level.',
    intermediate: 'Describe the Intermediate level.',
    advanced: 'Describe the Advanced level.',
    expert: 'Describe the Expert level.',
  });

  protected editDomain(domain: SkillDomain): void {
    this.editingDomain.set(domain);
    this.domainForm.setValue({ name: domain.name, description: domain.description ?? '' });
  }

  protected cancelDomain(): void {
    this.editingDomain.set(null);
    this.domainForm.reset();
  }

  protected saveDomain(): void {
    this.actionError.set(null);
    const editing = this.editingDomain();
    const value = this.domainForm.getRawValue();
    const request = { name: value.name, description: value.description || null };
    this.domainSubmission.submit(
      () =>
        editing
          ? this.organisation.updateSkillDomain(editing.id, request)
          : this.organisation.createSkillDomain(request),
      () => {
        this.cancelDomain();
        this.framework.reload();
      },
    );
  }

  protected removeDomain(domain: SkillDomain): void {
    this.remove(this.organisation.deleteSkillDomain(domain.id));
  }

  protected editSkill(skill: Skill): void {
    this.editingSkill.set(skill);
    this.skillForm.setValue({
      domainId: skill.domainId,
      name: skill.name,
      description: skill.description ?? '',
      ...skill.levels,
    });
  }

  protected cancelSkill(): void {
    this.editingSkill.set(null);
    this.skillForm.reset();
  }

  protected saveSkill(): void {
    this.actionError.set(null);
    const editing = this.editingSkill();
    const value = this.skillForm.getRawValue();
    const request = {
      domainId: value.domainId,
      name: value.name,
      description: value.description || null,
      levels: {
        beginner: value.beginner,
        intermediate: value.intermediate,
        advanced: value.advanced,
        expert: value.expert,
      },
    };
    this.skillSubmission.submit(
      () =>
        editing
          ? this.organisation.updateSkill(editing.id, request)
          : this.organisation.createSkill(request),
      () => {
        this.cancelSkill();
        this.framework.reload();
      },
    );
  }

  protected removeSkill(skill: Skill): void {
    this.remove(this.organisation.deleteSkill(skill.id));
  }

  private remove(request: ReturnType<OrganisationService['deleteSkill']>): void {
    this.actionError.set(null);
    request.subscribe({
      next: () => this.framework.reload(),
      error: (error: unknown) => this.actionError.set(toFormErrors(error).form),
    });
  }
}
