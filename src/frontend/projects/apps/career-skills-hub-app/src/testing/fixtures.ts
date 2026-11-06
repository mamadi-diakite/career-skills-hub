import {
  Department,
  EmployeeDetails,
  EmployeeSummary,
  Job,
  SkillDomain,
} from '@career-skills-hub/shared/core';

export const operations: Department = { id: 'department-ops', name: 'Operations' };
export const people: Department = { id: 'department-people', name: 'People' };
export const departments = [operations, people];

export const routePlanning = {
  id: 'skill-routes',
  domainId: 'domain-logistics',
  name: 'Route planning',
  description: 'Plans delivery rounds.',
  levels: {
    beginner: 'Follows a planned round.',
    intermediate: 'Plans a simple round.',
    advanced: 'Plans rounds for a depot.',
    expert: 'Designs the planning rules.',
  },
};

export const forkliftDriving = {
  id: 'skill-forklift',
  domainId: 'domain-logistics',
  name: 'Forklift driving',
  description: null,
  levels: {
    beginner: 'Drives with supervision.',
    intermediate: 'Drives alone.',
    advanced: 'Handles high racks.',
    expert: 'Trains other drivers.',
  },
};

export const skillDomains: SkillDomain[] = [
  {
    id: 'domain-logistics',
    name: 'Logistics',
    description: 'Moving goods.',
    skills: [forkliftDriving, routePlanning],
  },
  { id: 'domain-people', name: 'People skills', description: null, skills: [] },
];

export const dispatcher: Job = {
  id: 'job-dispatcher',
  title: 'Dispatcher',
  departmentId: operations.id,
  departmentName: operations.name,
  description: 'Plans the daily rounds.',
  requiredSkills: [
    {
      skillId: routePlanning.id,
      skillName: routePlanning.name,
      expectedLevel: 'Advanced',
      criticality: 'High',
    },
  ],
};

export const warehouseLead: Job = {
  id: 'job-lead',
  title: 'Warehouse lead',
  departmentId: operations.id,
  departmentName: operations.name,
  description: null,
  requiredSkills: [],
};

export const jobs = [dispatcher, warehouseLead];

export const marc: EmployeeSummary = {
  id: 'employee-marc',
  firstName: 'Marc',
  lastName: 'Lefebvre',
  email: 'marc.lefebvre@example.com',
  hireDate: '2019-03-04',
  jobId: warehouseLead.id,
  jobTitle: warehouseLead.title,
  departmentId: operations.id,
  departmentName: operations.name,
  managerId: null,
  managerName: null,
  accountRole: 'Manager',
};

export const ines: EmployeeSummary = {
  id: 'employee-ines',
  firstName: 'Ines',
  lastName: 'Moreau',
  email: 'ines.moreau@example.com',
  hireDate: '2022-09-01',
  jobId: dispatcher.id,
  jobTitle: dispatcher.title,
  departmentId: operations.id,
  departmentName: operations.name,
  managerId: marc.id,
  managerName: 'Marc Lefebvre',
  accountRole: null,
};

export const employees = [marc, ines];

export const inesDetails: EmployeeDetails = {
  employee: ines,
  jobHistory: [
    {
      jobId: dispatcher.id,
      jobTitle: dispatcher.title,
      departmentId: operations.id,
      departmentName: operations.name,
      startDate: '2024-01-01',
      endDate: null,
    },
    {
      jobId: warehouseLead.id,
      jobTitle: 'Warehouse operator',
      departmentId: operations.id,
      departmentName: operations.name,
      startDate: '2022-09-01',
      endDate: '2023-12-31',
    },
  ],
};
