import { Routes } from '@angular/router';
import { authGuard, guestGuard } from '@career-skills-hub/shared/core';
import { ChangePasswordPage } from './auth/change-password-page';
import { LoginPage } from './auth/login-page';
import { SignUpPage } from './auth/sign-up-page';
import { CompanyPage } from './company/company-page';
import { HomePage } from './home/home-page';

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
  { path: '**', redirectTo: '' },
];
