import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@career-skills-hub/shared/environment';
import { Observable } from 'rxjs';
import { UserRole } from '../auth/auth.models';
import {
  CreateEmployeeRequest,
  EmployeeDetails,
  EmployeeSummary,
  Invitation,
  JobChangeRequest,
} from './employees.models';

/** The API only returns the employees the signed-in user may see. */
@Injectable({ providedIn: 'root' })
export class EmployeesService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(APP_ENVIRONMENT).apiBaseUrl}/employees`;

  list(): Observable<EmployeeSummary[]> {
    return this.http.get<EmployeeSummary[]>(this.url);
  }

  get(id: string): Observable<EmployeeDetails> {
    return this.http.get<EmployeeDetails>(`${this.url}/${id}`);
  }

  create(request: CreateEmployeeRequest): Observable<EmployeeDetails> {
    return this.http.post<EmployeeDetails>(this.url, request);
  }

  changeJob(id: string, request: JobChangeRequest): Observable<EmployeeDetails> {
    return this.http.post<EmployeeDetails>(`${this.url}/${id}/job-changes`, request);
  }

  invite(id: string, role: UserRole): Observable<Invitation> {
    return this.http.post<Invitation>(`${this.url}/${id}/invitation`, { role });
  }
}
