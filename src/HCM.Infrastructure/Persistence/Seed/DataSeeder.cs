using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HCM.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await db.Database.MigrateAsync();
            await SeedRolesAsync(db);
            await SeedAdminUserAsync(db);
            await SeedDemoUsersAsync(db);
            await SeedCaseTypesAsync(db);
            await SeedCaseTagsAsync(db);
            await SeedProvidersAsync(db);
            await SeedDemoDataAsync(db);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(ApplicationDbContext db)
    {
        foreach (var roleName in RoleNames.All)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == roleName))
                db.Roles.Add(new Role { Name = roleName });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedAdminUserAsync(ApplicationDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Username == "admin"))
            return;

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@hcms.local",
            FirstName = "System",
            LastName = "Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123!"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var adminRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.Admin);
        db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoUsersAsync(ApplicationDbContext db)
    {
        var demoUsers = new[]
        {
            ("sarah.jones", "Sarah",   "Jones",   "sarah.jones@hcms.local",   RoleNames.CareCoordinator),
            ("michael.lee", "Michael", "Lee",     "michael.lee@hcms.local",   RoleNames.CareCoordinator),
            ("dr.patel",    "Priya",   "Patel",   "priya.patel@hcms.local",   RoleNames.Clinician),
            ("dr.carter",   "James",   "Carter",  "james.carter@hcms.local",  RoleNames.Clinician),
            ("lisa.chen",   "Lisa",    "Chen",    "lisa.chen@hcms.local",     RoleNames.Supervisor),
        };

        foreach (var (username, first, last, email, roleName) in demoUsers)
        {
            if (await db.Users.AnyAsync(u => u.Username == username))
                continue;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                FirstName = first,
                LastName = last,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var role = await db.Roles.FirstAsync(r => r.Name == roleName);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedCaseTypesAsync(ApplicationDbContext db)
    {
        var types = new[]
        {
            "Chronic Disease",
            "Post-Surgery",
            "Mental Health",
            "Preventive",
            "Behavioral"
        };

        foreach (var name in types)
        {
            if (!await db.CaseTypes.AnyAsync(ct => ct.Name == name))
                db.CaseTypes.Add(new CaseType { Name = name, IsActive = true, CreatedAt = DateTime.UtcNow });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedCaseTagsAsync(ApplicationDbContext db)
    {
        var tags = new[]
        {
            "Urgent",
            "High Priority",
            "Pediatric",
            "Geriatric",
            "Chronic Condition",
            "Post-Surgery",
            "Mental Health",
            "Preventive Care",
            "Follow-Up Required",
            "Complex Case"
        };

        foreach (var name in tags)
        {
            if (!await db.CaseTags.AnyAsync(t => t.Name == name))
                db.CaseTags.Add(new CaseTag { Name = name, IsActive = true, CreatedAt = DateTime.UtcNow });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedProvidersAsync(ApplicationDbContext db)
    {
        var providers = new[]
        {
            ("Dr. Priya Patel",   "Internal Medicine"),
            ("Dr. James Carter",  "Cardiology"),
            ("Dr. Sarah Jones",   "Psychiatry"),
            ("Dr. Michael Lee",   "Orthopedics"),
            ("Dr. Lisa Chen",     "Pediatrics")
        };

        foreach (var (name, specialty) in providers)
        {
            if (!await db.Providers.AnyAsync(p => p.Name == name))
                db.Providers.Add(new Provider { Name = name, Specialty = specialty, IsActive = true });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoDataAsync(ApplicationDbContext db)
    {
        // Idempotent — skip if 20+ patients already exist
        if (await db.Patients.CountAsync() >= 20) return;

        var admin     = await db.Users.FirstAsync(u => u.Username == "admin");
        var sarah     = await db.Users.FirstAsync(u => u.Username == "sarah.jones");
        var michael   = await db.Users.FirstAsync(u => u.Username == "michael.lee");
        var drPatel   = await db.Users.FirstAsync(u => u.Username == "dr.patel");
        var drCarter  = await db.Users.FirstAsync(u => u.Username == "dr.carter");

        var caseTypes = await db.CaseTypes.ToListAsync();
        var providers = await db.Providers.ToListAsync();

        var now = DateTime.UtcNow;
        var yr  = now.Year;

        // Determine next available sequence numbers to avoid collisions
        var lastMrnRaw = await db.Patients
            .Where(p => p.MRN.StartsWith($"MRN-{yr}-"))
            .OrderByDescending(p => p.MRN)
            .Select(p => p.MRN)
            .FirstOrDefaultAsync();
        int mrnStart = 1;
        if (lastMrnRaw != null && int.TryParse(lastMrnRaw[(lastMrnRaw.LastIndexOf('-') + 1)..], out int lastMrnNum))
            mrnStart = lastMrnNum + 1;

        var lastCaseRaw = await db.Cases
            .Where(c => c.CaseNumber.StartsWith($"CASE-{yr}-"))
            .OrderByDescending(c => c.CaseNumber)
            .Select(c => c.CaseNumber)
            .FirstOrDefaultAsync();
        int caseStart = 1;
        if (lastCaseRaw != null && int.TryParse(lastCaseRaw[(lastCaseRaw.LastIndexOf('-') + 1)..], out int lastCaseNum))
            caseStart = lastCaseNum + 1;

        // ── 20 Patients ──────────────────────────────────────────────────────
        (string First, string Last, DateTime DOB, string Gender, string Phone, string City, string State)[] pd =
        [
            ("John",      "Smith",      new(1965, 3, 15),  "Male",   "555-0101", "New York",       "NY"),
            ("Mary",      "Johnson",    new(1978, 7, 22),  "Female", "555-0102", "Los Angeles",    "CA"),
            ("Robert",    "Williams",   new(1952, 11, 8),  "Male",   "555-0103", "Chicago",        "IL"),
            ("Patricia",  "Brown",      new(1989, 4, 30),  "Female", "555-0104", "Houston",        "TX"),
            ("James",     "Davis",      new(1971, 9, 12),  "Male",   "555-0105", "Phoenix",        "AZ"),
            ("Linda",     "Miller",     new(1963, 1, 25),  "Female", "555-0106", "Philadelphia",   "PA"),
            ("Michael",   "Wilson",     new(1980, 6, 17),  "Male",   "555-0107", "San Antonio",    "TX"),
            ("Barbara",   "Moore",      new(1945, 12, 3),  "Female", "555-0108", "San Diego",      "CA"),
            ("William",   "Taylor",     new(1987, 8, 28),  "Male",   "555-0109", "Dallas",         "TX"),
            ("Elizabeth", "Anderson",   new(1956, 2, 14),  "Female", "555-0110", "San Jose",       "CA"),
            ("David",     "Thomas",     new(1993, 5, 19),  "Male",   "555-0111", "Austin",         "TX"),
            ("Jennifer",  "Jackson",    new(1968, 10, 7),  "Female", "555-0112", "Jacksonville",   "FL"),
            ("Richard",   "White",      new(1975, 3, 23),  "Male",   "555-0113", "Fort Worth",     "TX"),
            ("Maria",     "Harris",     new(1991, 7, 11),  "Female", "555-0114", "Columbus",       "OH"),
            ("Charles",   "Martin",     new(1949, 11, 29), "Male",   "555-0115", "Charlotte",      "NC"),
            ("Susan",     "Thompson",   new(1982, 4, 6),   "Female", "555-0116", "Indianapolis",   "IN"),
            ("Joseph",    "Garcia",     new(1961, 8, 14),  "Male",   "555-0117", "San Francisco",  "CA"),
            ("Margaret",  "Martinez",   new(1977, 1, 31),  "Female", "555-0118", "Seattle",        "WA"),
            ("Thomas",    "Robinson",   new(1988, 6, 20),  "Male",   "555-0119", "Denver",         "CO"),
            ("Dorothy",   "Clark",      new(1954, 9, 2),   "Female", "555-0120", "Nashville",      "TN"),
        ];

        var patients = new List<Patient>();
        for (int i = 0; i < pd.Length; i++)
        {
            var p = pd[i];
            patients.Add(new Patient
            {
                Id         = Guid.NewGuid(),
                MRN        = $"MRN-{yr}-{(mrnStart + i):D5}",
                FirstName  = p.First,
                LastName   = p.Last,
                DateOfBirth = p.DOB,
                Gender     = p.Gender,
                Phone      = p.Phone,
                Email      = $"{p.First.ToLower()}.{p.Last.ToLower()}@demo.com",
                City       = p.City,
                State      = p.State,
                IsActive   = true,
                CreatedBy  = admin.Id,
                CreatedAt  = now.AddDays(-60 + i),
                UpdatedAt  = now.AddDays(-60 + i)
            });
        }
        db.Patients.AddRange(patients);
        await db.SaveChangesAsync();

        // ── 20 Cases (one per patient) ────────────────────────────────────────
        string[] caseStatuses = [CaseStatus.Open, CaseStatus.InProgress, CaseStatus.InProgress,
                                  CaseStatus.OnHold, CaseStatus.Closed];
        Guid[] coordinators   = [sarah.Id, michael.Id];

        var cases = new List<Case>();
        for (int i = 0; i < patients.Count; i++)
        {
            var status = caseStatuses[i % caseStatuses.Length];
            cases.Add(new Case
            {
                Id               = Guid.NewGuid(),
                CaseNumber       = $"CASE-{yr}-{(caseStart + i):D5}",
                PatientId        = patients[i].Id,
                CaseTypeId       = caseTypes[i % caseTypes.Count].Id,
                CurrentStatus    = status,
                OpenedAt         = now.AddDays(-50 + i),
                ClosedAt         = status == CaseStatus.Closed ? now.AddDays(-5 + i) : null,
                AssignedToUserId = coordinators[i % 2],
                IsActive         = true,
                CreatedBy        = admin.Id,
                CreatedAt        = now.AddDays(-50 + i),
                UpdatedAt        = now.AddDays(-50 + i)
            });
        }
        db.Cases.AddRange(cases);
        await db.SaveChangesAsync();

        // ── 25 Tasks (every 4th case gets 2 tasks, rest get 1) ───────────────
        string[] taskTitles =
        [
            "Schedule follow-up appointment",
            "Review lab results with patient",
            "Complete medication reconciliation",
            "Coordinate specialist referral",
            "Update care plan documentation",
            "Confirm insurance pre-authorization",
            "Conduct patient education session",
            "Arrange home health services visit",
            "Review discharge summary notes",
            "Follow up on outstanding test results",
        ];
        string[] taskPriorities = ["Low", "Medium", "Medium", "High", "High"];
        string[] taskStatuses   = [CaseTaskStatus.Todo, CaseTaskStatus.InProgress, CaseTaskStatus.InProgress,
                                   CaseTaskStatus.Done, CaseTaskStatus.Blocked];
        Guid[] assignees        = [sarah.Id, michael.Id, drPatel.Id, drCarter.Id];

        var tasks    = new List<CaseTask>();
        int taskIdx  = 0;
        for (int i = 0; i < cases.Count; i++)
        {
            int numTasks = (i % 4 == 0) ? 2 : 1;
            for (int t = 0; t < numTasks; t++)
            {
                // Some tasks are overdue (due date in the past)
                int dueOffset = (i % 3 == 0) ? -(5 + i) : (7 + i);
                tasks.Add(new CaseTask
                {
                    Id          = Guid.NewGuid(),
                    CaseId      = cases[i].Id,
                    Title       = taskTitles[taskIdx % taskTitles.Length],
                    Description = $"Demo task for case {cases[i].CaseNumber}.",
                    Status      = taskStatuses[i % taskStatuses.Length],
                    Priority    = taskPriorities[i % taskPriorities.Length],
                    AssignedTo  = assignees[i % assignees.Length],
                    DueDate     = now.AddDays(dueOffset),
                    CreatedBy   = admin.Id,
                    CreatedAt   = now.AddDays(-40 + i),
                    UpdatedAt   = now.AddDays(-40 + i)
                });
                taskIdx++;
            }
        }
        db.CaseTasks.AddRange(tasks);
        await db.SaveChangesAsync();

        // ── 20 Appointments (one per patient, spread past and future) ─────────
        string[] apptTypes   = ["Initial Assessment", "Follow-Up", "Medication Review",
                                 "Lab Work", "Specialist Consultation", "Telehealth"];
        string[] apptStatuses = [AppointmentStatus.Scheduled, AppointmentStatus.Confirmed,
                                  AppointmentStatus.Completed, AppointmentStatus.Completed,
                                  AppointmentStatus.NoShow];

        var appointments = new List<Appointment>();
        for (int i = 0; i < patients.Count; i++)
        {
            var apptDate = now.AddDays(-25 + i * 3).Date; // spreads ~-25d to +32d
            var status   = apptDate < now.Date
                ? apptStatuses[(i + 2) % apptStatuses.Length]   // past → completed/no-show
                : apptStatuses[i % 2];                          // future → scheduled/confirmed

            appointments.Add(new Appointment
            {
                Id              = Guid.NewGuid(),
                PatientId       = patients[i].Id,
                CaseId          = cases[i].Id,
                ProviderId      = providers[i % providers.Count].Id,
                AppointmentType = apptTypes[i % apptTypes.Length],
                Status          = status,
                ScheduledStart  = apptDate.AddHours(8 + (i % 9)),
                ScheduledEnd    = apptDate.AddHours(8 + (i % 9) + 1),
                Location        = i % 3 == 0 ? "Telehealth" : $"Room {100 + i}",
                Notes           = $"Demo appointment for {patients[i].FirstName} {patients[i].LastName}.",
                CreatedBy       = admin.Id,
                CreatedAt       = now.AddDays(-45 + i),
                UpdatedAt       = now.AddDays(-45 + i)
            });
        }
        db.Appointments.AddRange(appointments);
        await db.SaveChangesAsync();
    }
}
