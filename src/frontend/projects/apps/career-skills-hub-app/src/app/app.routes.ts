import { Routes } from '@angular/router';
import { authGuard, guestGuard } from '@career-skills-hub/shared/core';
import { ChangePasswordPage } from './auth/change-password-page';
import { LoginPage } from './auth/login-page';
import { SignUpPage } from './auth/sign-up-page';
import { CompanyPage } from './company/company-page';
import { EmployeePage } from './employees/employee-page';
import { EmployeesPage } from './employees/employees-page';
import { HomePage } from './home/home-page';
import { JobPage } from './jobs/job-page';
import { JobsPage } from './jobs/jobs-page';
import { DepartmentsPage } from './organisation/departments-page';
import { SkillsPage } from './skills/skills-page';

export const routes: Routes = [
  { path: '', component: HomePage, title: 'Career Skills Hub' },
  {
    path: 'login',
    component: LoginPage,
    canActivate: [guestGuard],
    title: 'Log in · Career Skills Hub',
  },
  {
    path: 'sign-up',
    component: SignUpPage,
    canActivate: [guestGuard],
    title: 'Create your company · Career Skills Hub',
  },
  {
    path: 'account/password',
    component: ChangePasswordPage,
    canActivate: [authGuard],
    title: 'Change password · Career Skills Hub',
  },
  {
    path: 'company',
    component: CompanyPage,
    canActivate: [authGuard],
    title: 'Company profile · Career Skills Hub',
  },
  {
    path: 'departments',
    component: DepartmentsPage,
    canActivate: [authGuard],
    title: 'Departments · Career Skills Hub',
  },
  {
    path: 'skills',
    component: SkillsPage,
    canActivate: [authGuard],
    title: 'Skills framework · Career Skills Hub',
  },
  {
    path: 'jobs',
    component: JobsPage,
    canActivate: [authGuard],
    title: 'Jobs · Career Skills Hub',
  },
  {
    path: 'jobs/:id',
    component: JobPage,
    canActivate: [authGuard],
    title: 'Job · Career Skills Hub',
  },
  {
    path: 'employees',
    component: EmployeesPage,
    canActivate: [authGuard],
    title: 'Employees · Career Skills Hub',
  },
  {
    path: 'employees/:id',
    component: EmployeePage,
    canActivate: [authGuard],
    title: 'Employee · Career Skills Hub',
  },
  { path: '**', redirectTo: '' },
];
