import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@career-skills-hub/shared/environment';
import { Observable } from 'rxjs';

export interface Company {
  readonly id: string;
  readonly name: string;
  readonly headcount: number;
}

export interface UpdateCompanyRequest {
  readonly name: string;
  readonly headcount: number;
}

/** Profile of the company of the signed-in user; only HR admins can change it. */
@Injectable({ providedIn: 'root' })
export class CompanyService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(APP_ENVIRONMENT).apiBaseUrl}/company`;

  get(): Observable<Company> {
    return this.http.get<Company>(this.url);
  }

  update(request: UpdateCompanyRequest): Observable<Company> {
    return this.http.put<Company>(this.url, request);
  }
}
