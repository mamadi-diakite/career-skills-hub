namespace CareerSkillsHub.Infrastructure.Seeding;

/// <summary>A fictional logistics company. Every name, email and date is made up.</summary>
public static class DemoCompany
{
    public const string Name = "Blue Harbour Logistics";
    public const int Headcount = 64;

    public static IReadOnlyList<string> Departments { get; } = ["Warehouse", "Transport", "Maintenance", "Human Resources"];

    public static IReadOnlyList<DemoSkillDomain> SkillDomains { get; } =
    [
        new("Safety", "Working safely on site and on the road.",
        [
            new("Forklift driving", "Driving a counterbalance forklift in the warehouse.",
                "Drives with supervision on simple moves.",
                "Drives alone and loads standard racks.",
                "Handles high racks, narrow aisles and heavy loads.",
                "Trains and checks other drivers."),
            new("Workplace safety rules", "Applying the site safety rules and reporting hazards.",
                "Knows the main rules and wears protective equipment.",
                "Applies the rules without reminders.",
                "Spots hazards and proposes fixes.",
                "Leads safety briefings and audits."),
        ]),
        new("Logistics", "Moving and storing goods.",
        [
            new("Stock management", "Receiving, storing and counting goods.",
                "Scans and stores goods with help.",
                "Runs receiving and stock counts alone.",
                "Solves stock gaps and organises locations.",
                "Designs the storage plan of the site."),
            new("Route planning", "Planning delivery rounds.",
                "Follows a prepared round.",
                "Adapts a round to traffic and delays.",
                "Plans several rounds for the team.",
                "Optimises the delivery network."),
            new("Load securing", "Loading a truck and securing the cargo.",
                "Loads with a checklist and help.",
                "Loads and straps standard pallets alone.",
                "Handles mixed and fragile loads.",
                "Sets the loading rules for the site."),
        ]),
        new("Management", "Leading people and running reviews.",
        [
            new("Team leadership", "Organising the work of a team.",
                "Shares daily tasks.",
                "Plans the week and handles absences.",
                "Develops people and solves conflicts.",
                "Coaches other team leads."),
            new("Conducting reviews", "Preparing and running review meetings.",
                "Takes part in their own review.",
                "Runs reviews with a template.",
                "Turns reviews into clear actions.",
                "Trains managers to run reviews."),
        ]),
        new("Technical", "Keeping equipment running.",
        [
            new("Preventive maintenance", "Servicing equipment before it fails.",
                "Follows a service checklist with help.",
                "Services equipment alone.",
                "Plans the service schedule.",
                "Improves the maintenance programme."),
        ]),
        new("Human resources", "Running HR processes.",
        [
            new("Training planning", "Collecting needs and planning training.",
                "Records training requests.",
                "Builds the yearly plan from requests.",
                "Balances needs, budget and schedule.",
                "Sets the training policy."),
        ]),
    ];

    public static IReadOnlyList<DemoJob> Jobs { get; } =
    [
        new("Forklift operator", "Warehouse", "Moves pallets between docks and racks.",
        [
            new("Forklift driving", SkillLevel.Advanced, Criticality.High),
            new("Workplace safety rules", SkillLevel.Intermediate, Criticality.High),
            new("Stock management", SkillLevel.Beginner, Criticality.Low),
        ]),
        new("Warehouse team lead", "Warehouse", "Runs a warehouse shift and its team.",
        [
            new("Team leadership", SkillLevel.Advanced, Criticality.High),
            new("Stock management", SkillLevel.Advanced, Criticality.Medium),
            new("Workplace safety rules", SkillLevel.Advanced, Criticality.High),
            new("Conducting reviews", SkillLevel.Intermediate, Criticality.Medium),
        ]),
        new("Truck driver", "Transport", "Delivers customers on regional rounds.",
        [
            new("Route planning", SkillLevel.Intermediate, Criticality.Medium),
            new("Load securing", SkillLevel.Advanced, Criticality.High),
            new("Workplace safety rules", SkillLevel.Intermediate, Criticality.High),
        ]),
        new("Maintenance technician", "Maintenance", "Services forklifts, docks and conveyors.",
        [
            new("Preventive maintenance", SkillLevel.Advanced, Criticality.High),
            new("Workplace safety rules", SkillLevel.Advanced, Criticality.High),
        ]),
        new("HR officer", "Human Resources", "Runs reviews, training and HR records.",
        [
            new("Training planning", SkillLevel.Advanced, Criticality.High),
            new("Conducting reviews", SkillLevel.Advanced, Criticality.Medium),
        ]),
    ];

    /// <summary>Managers come before the people they manage.</summary>
    public static IReadOnlyList<DemoEmployee> Employees { get; } =
    [
        new("Claire", "Fontaine", "claire.fontaine@example.com", new(2019, 4, 1), "HR officer", "Human Resources",
            ManagerEmail: null, AccountRole: UserRole.HrAdmin),
        new("Marc", "Lefebvre", "marc.lefebvre@example.com", new(2016, 9, 12), "Warehouse team lead", "Warehouse",
            ManagerEmail: null, AccountRole: UserRole.Manager,
            FirstJob: new("Forklift operator", "Warehouse", new(2020, 1, 6))),
        new("Ines", "Moreau", "ines.moreau@example.com", new(2022, 2, 7), "Forklift operator", "Warehouse",
            ManagerEmail: "marc.lefebvre@example.com", AccountRole: UserRole.Employee),
        new("Tom", "Girard", "tom.girard@example.com", new(2018, 6, 18), "Forklift operator", "Warehouse",
            ManagerEmail: "marc.lefebvre@example.com", AccountRole: null,
            FirstJob: new("Truck driver", "Transport", new(2021, 1, 4))),
        new("Lucas", "Bernard", "lucas.bernard@example.com", new(2024, 10, 1), "Forklift operator", "Warehouse",
            ManagerEmail: "marc.lefebvre@example.com", AccountRole: null),
        new("Nadia", "Haddad", "nadia.haddad@example.com", new(2020, 3, 2), "Truck driver", "Transport",
            ManagerEmail: null, AccountRole: null),
        new("Paul", "Garnier", "paul.garnier@example.com", new(2017, 11, 20), "Maintenance technician", "Maintenance",
            ManagerEmail: null, AccountRole: null),
    ];
}

public sealed record DemoSkillDomain(string Name, string Description, IReadOnlyList<DemoSkill> Skills);

public sealed record DemoSkill(string Name, string Description, string Beginner, string Intermediate, string Advanced, string Expert);

public sealed record DemoJob(string Title, string Department, string Description, IReadOnlyList<DemoRequiredSkill> RequiredSkills);

public sealed record DemoRequiredSkill(string Skill, SkillLevel ExpectedLevel, Criticality Criticality);

/// <param name="AccountRole">Role of the login account, or <c>null</c> when the employee has none.</param>
/// <param name="FirstJob">Job held from the hire date until the current job started.</param>
public sealed record DemoEmployee(
    string FirstName,
    string LastName,
    string Email,
    DateOnly HireDate,
    string Job,
    string Department,
    string? ManagerEmail,
    UserRole? AccountRole,
    DemoFirstJob? FirstJob = null);

/// <param name="CurrentJobFrom">Start date of the current job.</param>
public sealed record DemoFirstJob(string Job, string Department, DateOnly CurrentJobFrom);
