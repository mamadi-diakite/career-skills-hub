import { UserRole } from '../auth/auth.models';

export const USER_ROLES: readonly { readonly value: UserRole; readonly label: string }[] = [
  { value: 'Employee', label: 'Employee' },
  { value: 'Manager', label: 'Manager' },
  { value: 'HrAdmin', label: 'HR admin' },
];

export function roleLabel(role: UserRole): string {
  return USER_ROLES.find((option) => option.value === role)!.label;
}

export interface EmployeeSummary {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  /** ISO date, e.g. `2022-09-01`. */
  readonly hireDate: string;
  readonly jobId: string;
  readonly jobTitle: string;
  readonly departmentId: string;
  readonly departmentName: string;
  readonly managerId: string | null;
  readonly managerName: string | null;
  /** `null` while the employee has not been invited. */
  readonly accountRole: UserRole | null;
}

export interface JobAssignment {
  readonly jobId: string;
  readonly jobTitle: string;
  readonly departmentId: string;
  readonly departmentName: string;
  readonly startDate: string;
  /** Last day in the job, or `null` for the current job. */
  readonly endDate: string | null;
}

export interface EmployeeDetails {
  readonly employee: EmployeeSummary;
  /** Most recent first. */
  readonly jobHistory: readonly JobAssignment[];
}

export interface CreateEmployeeRequest {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly hireDate: string;
  readonly jobId: string;
  readonly departmentId: string;
  readonly managerId: string | null;
}

export interface JobChangeRequest {
  readonly jobId: string;
  readonly departmentId: string;
  readonly effectiveDate: string;
}

export interface Invitation {
  readonly employeeId: string;
  readonly email: string;
  readonly role: UserRole;
  /** Shown once; the employee changes it after the first login. */
  readonly temporaryPassword: string;
}
