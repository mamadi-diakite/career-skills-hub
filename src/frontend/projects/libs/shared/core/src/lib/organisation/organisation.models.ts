export const SKILL_LEVELS = ['Beginner', 'Intermediate', 'Advanced', 'Expert'] as const;
export type SkillLevel = (typeof SKILL_LEVELS)[number];

export const CRITICALITIES = ['Low', 'Medium', 'High'] as const;
export type Criticality = (typeof CRITICALITIES)[number];

export interface Department {
  readonly id: string;
  readonly name: string;
}

export interface DepartmentRequest {
  readonly name: string;
}

/** What each of the four levels means for a skill. */
export interface SkillLevelTexts {
  readonly beginner: string;
  readonly intermediate: string;
  readonly advanced: string;
  readonly expert: string;
}

export interface Skill {
  readonly id: string;
  readonly domainId: string;
  readonly name: string;
  readonly description: string | null;
  readonly levels: SkillLevelTexts;
}

export interface SkillDomain {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  /** Sorted by name. */
  readonly skills: readonly Skill[];
}

export interface SkillDomainRequest {
  readonly name: string;
  readonly description: string | null;
}

export interface SkillRequest {
  readonly domainId: string;
  readonly name: string;
  readonly description: string | null;
  readonly levels: SkillLevelTexts;
}

export interface RequiredSkill {
  readonly skillId: string;
  readonly skillName: string;
  readonly expectedLevel: SkillLevel;
  readonly criticality: Criticality;
}

export interface Job {
  readonly id: string;
  readonly title: string;
  readonly departmentId: string;
  readonly departmentName: string;
  readonly description: string | null;
  /** Most critical first, then by skill name. */
  readonly requiredSkills: readonly RequiredSkill[];
}

export interface JobRequest {
  readonly title: string;
  readonly departmentId: string;
  readonly description: string | null;
}

export interface RequiredSkillItem {
  readonly skillId: string;
  readonly expectedLevel: SkillLevel;
  readonly criticality: Criticality;
}
