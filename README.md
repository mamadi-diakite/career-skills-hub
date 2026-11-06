# career-skills-hub

A web app for a company to plan and follow the development of its employees: reviews, training, certifications and skills.

The company describes its jobs and the skills each job needs. Employees, managers and HR fill in questionnaires: self-assessments, annual reviews and the mandatory French career review ("entretien de parcours professionnel"), whose due dates the app computes. The app tracks training wishes, the yearly training plan, completed training and certifications to renew.

A dashboard shows skills coverage and the risks: jobs with nobody qualified, critical skills held by one person only, certifications about to expire. Each employee has a career plan toward the job they aim for.

## Who it is for

HR teams and managers in small and mid-sized companies who follow reviews, training and skills today with spreadsheets.

## What it is not

- Not payroll, recruiting or an e-learning platform.
- Not legal advice. The app computes review dates and flags risks; the company stays responsible for its obligations.
- No salary amounts and no health data.

## Stack

- Front end: Angular workspace (app + libraries)
- Back end: ASP.NET Core Web API (C#)
- Database: SQLite with EF Core

## Status

Early development (milestone M0). What works today:

- Create a company together with its first HR admin account, log in, change your password. Sessions expire after 60 minutes.
- Three roles with access checked by the API on every request: an employee sees their own record, a manager also sees their team, HR admins see and manage everyone.
- HR admins edit the company profile and manage the departments, the jobs and the skills framework: skill domains, skills and what each of the four levels (Beginner, Intermediate, Advanced, Expert) means.
- Each job lists its required skills with the expected level and a criticality (Low, Medium, High).
- HR admins add employees, record job changes (the job history is kept) and invite an employee by creating their account with a temporary password, shown once.

Assessments, reviews, training, certifications and the skills dashboard are planned (see [ROADMAP.md](ROADMAP.md)).

## Getting started

### Prerequisites

- [.NET SDK 10.0.403](https://dotnet.microsoft.com/download) or a later 10.0 feature band
- [Node.js 24](https://nodejs.org/) with npm 11

### Run the API

The access tokens are signed with a key that is never stored in the repository, and the API refuses to start without it. Set a base64 key of at least 32 bytes in the `Jwt__SigningKey` environment variable, then start the API from the same terminal.

Bash:

```bash
export Jwt__SigningKey="$(openssl rand -base64 32)"
cd src/backend
dotnet run --project src/CareerSkillsHub.Api --launch-profile http
```

PowerShell:

```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$env:Jwt__SigningKey = [Convert]::ToBase64String($bytes)
cd src/backend
dotnet run --project src/CareerSkillsHub.Api --launch-profile http
```

The API listens on http://localhost:5089. In Development it creates the SQLite database in `src/backend/src/CareerSkillsHub.Api/App_Data/`, applies the migrations, and adds the demo data below when the database has no users. A new key logs everybody out, so keep the same key between restarts if you want to stay signed in.

### Run the web app

```bash
cd src/frontend
npm ci
npm start
```

Open http://localhost:4200. Calls to `/api` are sent to the API on port 5089.

### Demo data

All demo accounts use the password `CareerDemo123!`. They are fictional and only exist in Development.

| Name | Email | Role | Job |
|------|-------|------|-----|
| Claire Fontaine | claire.fontaine@example.com | HR admin | HR officer |
| Marc Lefebvre | marc.lefebvre@example.com | Manager | Warehouse team lead |
| Ines Moreau | ines.moreau@example.com | Employee | Forklift operator |

The demo company, Blue Harbour Logistics, is a fictional logistics company with 4 departments, 5 jobs, 9 skills in 5 domains and 7 employees. Marc Lefebvre manages Ines Moreau, Tom Girard and Lucas Bernard. Tom Girard, Lucas Bernard, Nadia Haddad and Paul Garnier have no account yet: log in as Claire Fontaine to invite them.

To start again from an empty demo, stop the API and delete `src/backend/src/CareerSkillsHub.Api/App_Data/`.

### Run the tests

Back end (with coverage, migrations and generated code excluded). The tests set their own signing key, so `Jwt__SigningKey` is not needed:

```bash
cd src/backend
dotnet test --settings coverage.runsettings --collect "XPlat Code Coverage"
```

Front end (Vitest, the run fails below 100% coverage):

```bash
cd src/frontend
npm run test:ci
```

## Project layout

```
src/backend/    ASP.NET Core solution: Domain, Application.Contracts, Application, Infrastructure, Api, and their test projects
src/frontend/   Angular workspace: projects/apps/career-skills-hub-app and the shared libraries in projects/libs/shared
```

## License

[MIT](LICENSE)
