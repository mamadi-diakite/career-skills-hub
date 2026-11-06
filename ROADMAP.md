# Roadmap

This is the working plan for career-skills-hub. Stories are grouped by epic and tagged with the milestone they belong to.

## Scope

In scope:
- A company with departments, jobs, a skills framework and employees, with access rules for HR, managers and employees.
- Questionnaire templates, annual and career reviews with sign-off, skill assessments, and the career review schedule with its risk alerts.
- Training wishes, the yearly training plan and budget, certifications and renewals, the skills dashboard with risks, and career plans.

Out of scope:
- Not payroll, recruiting or an e-learning platform.
- Not legal advice. The app computes review dates and flags risks; the company stays responsible for its obligations.
- No salary amounts and no health data.
- No real employee data. The demo runs on a fictional company and fictional employees.

## Glossary

| Term | Meaning |
|------|---------|
| Company | The employer using the app. Its headcount matters for some rules |
| Department | A team or service inside the company |
| Job | A position in the company, for example "Forklift operator" |
| Skill | A know-how in a skill domain, assessed on four levels |
| Required skill | A skill a job needs, with the expected level and a criticality (low, medium, high) |
| Questionnaire template | A reusable set of questions: text, choice, scale, or skill to assess |
| Review | A meeting based on a questionnaire, filled in by the employee and the manager: annual review or career review |
| Career review | The mandatory review on career path and training needs ("entretien de parcours professionnel") |
| Summary review | The career review held every eight years that checks the past eight years |
| Corrective top-up | The 3,000 euro amount a company with 50 or more employees must pay into the employee's training account when the summary review finds no reviews and no non-mandatory training |
| Training | A course from the catalog, mandatory (required to do the job) or not |
| Training plan | The yearly list of planned training with its budget |
| Certification | A qualification with an expiry date, for example a forklift licence or first-aid training |
| Coverage | Whether a job's required skills are held at the expected level by enough people |
| Career plan | An employee's target job, skills gap and planned training |

## Roles

| Role | Can do |
|------|--------|
| HR admin | Manage the company, departments, jobs, skills, templates, training plan; see everyone |
| Manager | See and review the employees of their team, approve training wishes |
| Employee | See their own data, fill in their part of reviews, add training wishes |

## Milestones

| Milestone | Goal |
|-----------|------|
| M0 Foundation | Runnable app: accounts and roles, company, departments, jobs, skills framework, employees |
| M1 Reviews | Questionnaire templates, annual and career reviews with their workflow, skill assessments |
| M2 Career review schedule | Due dates, summary review checks, overdue and top-up risk alerts |
| M3 Training and certifications | Catalog, wishes, yearly plan and budget, completed training, certifications and renewals |
| M4 Dashboard and career plans | Skills matrix, coverage and risks, career plans with skills gap |
| M5 Quality | CI, tests, accessibility, docs |

---

## EPIC 1 — Project setup

| ID | Story | Milestone |
|----|-------|-----------|
| SETUP-1 | Repository with README, LICENSE (MIT), .gitignore, .editorconfig | M0 |
| SETUP-2 | .NET solution: Api, Domain, Application, Infrastructure and Tests projects | M0 |
| SETUP-3 | SQLite + EF Core, first migration, database created on startup in dev | M0 |
| SETUP-4 | Angular workspace with the main app and shared libraries | M0 |
| SETUP-5 | GitHub Actions: build and test both API and front end on each push | M5 |

## EPIC 2 — Accounts and access

| ID | Story | Milestone |
|----|-------|-----------|
| AUTH-1 | As a visitor, I create a company and its first HR admin account | M0 |
| AUTH-2 | As a user, I log in and get a token | M0 |
| AUTH-3 | As a user, I log out, and I am sent back to login when my session expires | M0 |
| AUTH-4 | As a user, I change my password | M0 |
| AUTH-5 | A demo company with departments, jobs, skills and employees is seeded with a documented demo password | M0 |
| AUTH-6 | Access rules: employees see their own data, managers their team, HR everyone; checked by the API on every request | M0 |

Acceptance notes:
- Passwords are hashed. Demo emails use `@example.com`.

## EPIC 3 — Company, jobs and skills

| ID | Story | Milestone |
|----|-------|-----------|
| ORG-1 | As HR, I edit the company profile, including its headcount | M0 |
| ORG-2 | As HR, I manage departments | M0 |
| ORG-3 | As HR, I manage jobs with a title, a department and a description | M0 |
| SKILL-1 | As HR, I manage skill domains and skills, each with a description of its four levels | M0 |
| SKILL-2 | As HR, I set the required skills of a job: expected level and criticality | M0 |
| EMP-1 | As HR, I add employees with hire date, job, department and manager, and invite them | M0 |
| EMP-2 | As HR, I record a job change with its date; the history is kept | M0 |
| EMP-3 | As HR, I import employees from a CSV file with a validation report | M3 |

## EPIC 4 — Questionnaires and reviews

| ID | Story | Milestone |
|----|-------|-----------|
| REVIEW-1 | As HR, I build questionnaire templates with sections and questions: text, single or multiple choice, scale, skill to assess | M1 |
| REVIEW-2 | Each question says who answers it: employee, manager or both | M1 |
| REVIEW-3 | As HR or manager, I schedule a review (annual or career) for an employee with a template and a date | M1 |
| REVIEW-4 | As an employee, I fill in my preparation part before the meeting | M1 |
| REVIEW-5 | As a manager, I fill in my part, see the employee's answers and add the agreed actions | M1 |
| REVIEW-6 | Both sign off the review; it is then locked and a PDF summary is available to both | M1 |
| REVIEW-7 | Skill questions update the employee's assessed level for that skill, with date and source | M1 |
| REVIEW-8 | As an employee, I self-assess my skills outside a review; my manager confirms or adjusts | M1 |

## EPIC 5 — Career review schedule

| ID | Story | Milestone |
|----|-------|-----------|
| CAREER-1 | The first career review is due within one year of the hire date | M2 |
| CAREER-2 | Next career reviews are due four years after the previous one | M2 |
| CAREER-3 | A summary review is due eight years after hiring, or seven years after the first career review, then every eight years | M2 |
| CAREER-4 | The summary review shows, for the past eight years: reviews held, training followed, certifications gained, and a pay or career progression question | M2 |
| CAREER-5 | In a company with 50 or more employees, the app flags a top-up risk when no reviews were held and no non-mandatory training was followed in the period | M2 |
| CAREER-6 | As HR, I see career reviews due soon, overdue and summary reviews with a top-up risk | M2 |
| CAREER-7 | As HR, I record a return from long leave; a career review is suggested when none took place in the year before the return | M2 |

Acceptance notes:
- The schedule is a pure function covered by unit tests: hire date, past reviews, training and headcount in, due dates and risks out.
- Dates follow article L6315-1 of the French labor code as amended in December 2025. The README links to the official text.

## EPIC 6 — Training

| ID | Story | Milestone |
|----|-------|-----------|
| TRAIN-1 | As HR, I manage a training catalog: title, provider, duration, cost, mandatory or not, skills it develops | M3 |
| TRAIN-2 | As an employee, I add a training wish with a reason | M3 |
| TRAIN-3 | As a manager, I approve or decline my team's wishes with a comment | M3 |
| TRAIN-4 | As HR, I build the yearly training plan from approved wishes and needs, with a budget | M3 |
| TRAIN-5 | As HR, I follow each planned training: planned, done, cancelled; the plan shows spent and remaining budget | M3 |
| TRAIN-6 | As HR, I record completed training, including training done before the app was used | M3 |
| TRAIN-7 | Completed training can raise skill levels after the manager confirms | M3 |

## EPIC 7 — Certifications

| ID | Story | Milestone |
|----|-------|-----------|
| CERT-1 | As HR, I manage certification types with their validity period | M3 |
| CERT-2 | As HR, I record an employee's certification with date obtained and expiry date | M3 |
| CERT-3 | A job can require a certification | M3 |
| CERT-4 | HR and managers get in-app reminders before a certification expires | M3 |
| CERT-5 | Renewing a certification links it to the training that renewed it | M3 |

## EPIC 8 — Dashboard and risks

| ID | Story | Milestone |
|----|-------|-----------|
| DASH-1 | As HR, I see the skills matrix: employees by required skill, with level against expected level | M4 |
| DASH-2 | Each job shows its coverage: how many people hold each required skill and certification at the expected level | M4 |
| DASH-3 | Risks are listed by severity: job with nobody qualified, critical skill held by one person only, certification expiring within 90 days that leaves a job uncovered | M4 |
| DASH-4 | As a manager, I see the same view for my team | M4 |
| DASH-5 | As HR, I see training plan progress and career review compliance | M4 |

Acceptance notes:
- Coverage and risk rules are pure functions covered by unit tests.

## EPIC 9 — Career plans

| ID | Story | Milestone |
|----|-------|-----------|
| PLAN-1 | As an employee, I record my career wishes: target job, mobility, timeframe | M4 |
| PLAN-2 | The app shows the skills gap between my current levels and the target job | M4 |
| PLAN-3 | The app suggests catalog training that develops the missing skills | M4 |
| PLAN-4 | As a manager, I agree a progression plan with steps and dates, reviewed at the next review | M4 |

## EPIC 10 — Quality and docs

| ID | Story | Milestone |
|----|-------|-----------|
| QA-1 | API integration tests for access rules, reviews, schedule and risks | M5 |
| QA-2 | Unit tests for schedule, coverage and risk rules with worked examples | M5 |
| QA-3 | End-to-end test: schedule a review, both sides fill it in, sign off, skill level updated (Playwright) | M5 |
| QA-4 | Accessibility pass on review forms | M5 |
| QA-5 | Docs: rules used, demo data, limits | M5 |

---

## Later ideas

- Email reminders and review invitations
- Company agreement settings: shorter review period, other summary review criteria
- Export of the training plan for the works council
- French and English UI (the domain is French, so a French UI comes first)
