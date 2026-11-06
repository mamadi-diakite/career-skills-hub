import { LoginResponse } from './auth.models';

/** A session of the demo HR admin, valid until `expiresAtUtc`. */
export function testSession(expiresAtUtc = '2999-01-01T00:00:00Z'): LoginResponse {
  return {
    accessToken: 'token-1',
    expiresAtUtc,
    user: {
      id: 'user-1',
      email: 'claire.fontaine@example.com',
      displayName: 'Claire Fontaine',
      role: 'HrAdmin',
      companyId: 'company-1',
      companyName: 'Blue Harbour Logistics',
      employeeId: null,
    },
  };
}
