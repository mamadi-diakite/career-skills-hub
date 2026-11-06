import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { APP_ENVIRONMENT } from '@career-skills-hub/shared/environment';
import { Observable } from 'rxjs';
import {
  Department,
  DepartmentRequest,
  Job,
  JobRequest,
  RequiredSkillItem,
  Skill,
  SkillDomain,
  SkillDomainRequest,
  SkillRequest,
} from './organisation.models';

/** Departments, jobs and the skills framework; every role reads them, HR admins change them. */
@Injectable({ providedIn: 'root' })
export class OrganisationService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(APP_ENVIRONMENT).apiBaseUrl;

  departments(): Observable<Department[]> {
    return this.http.get<Department[]>(`${this.apiBaseUrl}/departments`);
  }

  createDepartment(request: DepartmentRequest): Observable<Department> {
    return this.http.post<Department>(`${this.apiBaseUrl}/departments`, request);
  }

  updateDepartment(id: string, request: DepartmentRequest): Observable<Department> {
    return this.http.put<Department>(`${this.apiBaseUrl}/departments/${id}`, request);
  }

  deleteDepartment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/departments/${id}`);
  }

  skillDomains(): Observable<SkillDomain[]> {
    return this.http.get<SkillDomain[]>(`${this.apiBaseUrl}/skill-domains`);
  }

  createSkillDomain(request: SkillDomainRequest): Observable<SkillDomain> {
    return this.http.post<SkillDomain>(`${this.apiBaseUrl}/skill-domains`, request);
  }

  updateSkillDomain(id: string, request: SkillDomainRequest): Observable<SkillDomain> {
    return this.http.put<SkillDomain>(`${this.apiBaseUrl}/skill-domains/${id}`, request);
  }

  deleteSkillDomain(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/skill-domains/${id}`);
  }

  createSkill(request: SkillRequest): Observable<Skill> {
    return this.http.post<Skill>(`${this.apiBaseUrl}/skills`, request);
  }

  updateSkill(id: string, request: SkillRequest): Observable<Skill> {
    return this.http.put<Skill>(`${this.apiBaseUrl}/skills/${id}`, request);
  }

  deleteSkill(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/skills/${id}`);
  }

  jobs(): Observable<Job[]> {
    return this.http.get<Job[]>(`${this.apiBaseUrl}/jobs`);
  }

  job(id: string): Observable<Job> {
    return this.http.get<Job>(`${this.apiBaseUrl}/jobs/${id}`);
  }

  createJob(request: JobRequest): Observable<Job> {
    return this.http.post<Job>(`${this.apiBaseUrl}/jobs`, request);
  }

  updateJob(id: string, request: JobRequest): Observable<Job> {
    return this.http.put<Job>(`${this.apiBaseUrl}/jobs/${id}`, request);
  }

  /** Replaces all the required skills of the job. */
  setRequiredSkills(id: string, requiredSkills: readonly RequiredSkillItem[]): Observable<Job> {
    return this.http.put<Job>(`${this.apiBaseUrl}/jobs/${id}/required-skills`, {
      requiredSkills,
    });
  }

  deleteJob(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/jobs/${id}`);
  }
}
