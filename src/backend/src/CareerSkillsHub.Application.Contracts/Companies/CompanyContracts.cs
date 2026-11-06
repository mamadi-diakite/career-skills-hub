namespace CareerSkillsHub.Application.Contracts.Companies;

public sealed record CompanyResponse(Guid Id, string Name, int Headcount);

public sealed record UpdateCompanyRequest(string Name, int Headcount);
