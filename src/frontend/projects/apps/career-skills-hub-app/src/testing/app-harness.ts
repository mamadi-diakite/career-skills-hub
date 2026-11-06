import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { CurrentUser, LoginResponse, SessionStore, UserRole } from '@career-skills-hub/shared/core';
import { App } from '../app/app';
import { appConfig } from '../app/app.config';

const users: Record<UserRole, CurrentUser> = {
  HrAdmin: {
    id: 'user-hr',
    email: 'claire.fontaine@example.com',
    displayName: 'Claire Fontaine',
    role: 'HrAdmin',
    companyId: 'company-1',
    companyName: 'Blue Harbour Logistics',
    employeeId: 'employee-claire',
  },
  Manager: {
    id: 'user-manager',
    email: 'marc.lefebvre@example.com',
    displayName: 'Marc Lefebvre',
    role: 'Manager',
    companyId: 'company-1',
    companyName: 'Blue Harbour Logistics',
    employeeId: 'employee-marc',
  },
  Employee: {
    id: 'user-employee',
    email: 'ines.moreau@example.com',
    displayName: 'Ines Moreau',
    role: 'Employee',
    companyId: 'company-1',
    companyName: 'Blue Harbour Logistics',
    employeeId: 'employee-ines',
  },
};

export function sessionFor(role: UserRole, expiresAtUtc = '2999-01-01T00:00:00Z'): LoginResponse {
  return { accessToken: `token-${role}`, expiresAtUtc, user: users[role] };
}

/** The whole app on a fake API, with helpers to drive it like a user. */
export class AppHarness {
  readonly http: HttpTestingController;
  readonly router: Router;
  readonly fixture: ComponentFixture<App>;

  constructor() {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [...appConfig.providers, provideHttpClientTesting()],
    });
    this.http = TestBed.inject(HttpTestingController);
    this.router = TestBed.inject(Router);
    this.fixture = TestBed.createComponent(App);
    this.http.expectOne('/api/health').flush({ status: 'ok' });
  }

  get element(): HTMLElement {
    return this.fixture.nativeElement as HTMLElement;
  }

  signIn(role: UserRole = 'HrAdmin', expiresAtUtc?: string): void {
    TestBed.inject(SessionStore).start(sessionFor(role, expiresAtUtc));
  }

  async open(url: string): Promise<void> {
    await this.router.navigateByUrl(url);
    await this.fixture.whenStable();
  }

  async type(id: string, value: string): Promise<void> {
    const input = this.element.querySelector<HTMLInputElement>(`#${id}`)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    await this.fixture.whenStable();
  }

  async select(id: string, value: string): Promise<void> {
    const select = this.element.querySelector<HTMLSelectElement>(`#${id}`)!;
    select.value = value;
    select.dispatchEvent(new Event('change'));
    select.dispatchEvent(new Event('blur'));
    await this.fixture.whenStable();
  }

  async submit(form = 'form'): Promise<void> {
    this.element.querySelector<HTMLButtonElement>(`${form} button[type="submit"]`)!.click();
    await this.fixture.whenStable();
  }

  async click(selector: string): Promise<void> {
    this.element.querySelector<HTMLElement>(selector)!.click();
    await this.fixture.whenStable();
  }

  async settle(): Promise<void> {
    await this.fixture.whenStable();
  }

  input(id: string): HTMLInputElement {
    return this.element.querySelector<HTMLInputElement>(`#${id}`)!;
  }

  text(selector: string): string | undefined {
    return this.element.querySelector(selector)?.textContent?.trim();
  }

  texts(selector: string): string[] {
    return [...this.element.querySelectorAll(selector)].map((node) =>
      node.textContent!.trim().replace(/\s+/g, ' '),
    );
  }

  button(selector = 'form button[type="submit"]'): HTMLButtonElement {
    return this.element.querySelector<HTMLButtonElement>(selector)!;
  }
}
