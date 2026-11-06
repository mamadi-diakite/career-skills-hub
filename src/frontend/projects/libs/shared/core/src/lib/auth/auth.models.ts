export type UserRole = 'HrAdmin' | 'Manager' | 'Employee';

export interface CurrentUser {
  readonly id: string;
  readonly email: string;
  readonly displayName: string;
  readonly role: UserRole;
  readonly companyId: string;
  readonly companyName: string;
  /** Employee record of the user; always set for managers and employees. */
  readonly employeeId: string | null;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

/** Sign-up creates the company together with its first HR admin. */
export interface SignUpRequest {
  readonly companyName: string;
  readonly headcount: number;
  readonly displayName: string;
  readonly email: string;
  readonly password: string;
  readonly confirmPassword: string;
}

export interface ChangePasswordRequest {
  readonly currentPassword: string;
  readonly newPassword: string;
  readonly confirmPassword: string;
}

export interface LoginResponse {
  readonly accessToken: string;
  readonly expiresAtUtc: string;
  readonly user: CurrentUser;
}
