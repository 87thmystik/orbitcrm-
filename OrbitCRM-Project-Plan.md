OrbitCRM — Project Master Document
1. Executive Summary
OrbitCRM is a multi-tenant SaaS Customer Relationship Management platform built with .NET 10, Blazor Web App (Interactive Server), Entity Framework Core, and PostgreSQL 17. It enables multiple businesses (tenants) to manage customers, leads, sales pipelines, tasks, and reports — with strict tenant isolation enforced at the database, application, and query layers.

Primary objective: Ship a usable MVP covering core CRM workflows (customers, leads, deals, tasks, dashboard) before adding AI, invoicing, and messaging integrations.

2. Technology Stack
Layer	Technology
Runtime	.NET 10 (LTS)
Web framework	Blazor Web App — Interactive Server render mode
Database	PostgreSQL 17
ORM	Entity Framework Core 10 (Npgsql provider)
Authentication	ASP.NET Core Identity (Guid keys)
CSS	Tailwind CSS v4 (via @tailwindcss/cli)
Testing	xUnit
IDE	Visual Studio 2022+
Architecture pattern: Layered modular monolith (Clean Architecture-inspired).

3. Solution Structure
text
OrbitCRM/
├── OrbitCRM.slnx
├── src/
│   ├── OrbitCRM.Web/                 Blazor UI, pages, layout, DI entry point
│   ├── OrbitCRM.Application/         Interfaces, DTOs, business use-case contracts
│   ├── OrbitCRM.Domain/              Entities, enums, domain rules
│   └── OrbitCRM.Infrastructure/      EF Core, Identity, services, external integrations
└── tests/
    ├── OrbitCRM.UnitTests/           Unit tests for domain + services
    └── OrbitCRM.IntegrationTests/    End-to-end tests (later)
Dependency direction
text
Web ──► Infrastructure ──► Application ──► Domain
Web ──► Application
Rule: Domain knows nothing. Application knows only Domain. Infrastructure implements Application interfaces. Web wires everything together.

4. Class Diagram — Domain Layer
text
                          ┌──────────────────────┐
                          │     TenantEntity     │  (abstract)
                          ├──────────────────────┤
                          │ + Id: Guid           │
                          │ + TenantId: Guid     │
                          │ + CreatedAt: DateTime│
                          │ + UpdatedAt: DateTime│
                          │ + IsDeleted: bool    │
                          └──────────▲───────────┘
                                     │ inherits
        ┌────────────────┬───────────┼────────────┬──────────────┐
        │                │           │            │              │
   ┌────┴─────┐    ┌─────┴────┐  ┌───┴─────┐ ┌────┴─────┐ ┌──────┴──────┐
   │ Customer │    │  Lead    │  │  Deal   │ │ TaskItem │ │ CrmActivity │
   └──────────┘    └──────────┘  └─────────┘ └──────────┘ └─────────────┘
                                       │            │             │
                                       │ refs       │ refs        │ refs
                                       ▼            ▼             ▼
                              ┌─────────────────┐
                              │ PipelineStage   │
                              └─────────────────┘
                              ┌─────────────────┐
                              │    Tenant       │  (root — no TenantId)
                              └─────────────────┘
                              ┌─────────────────┐
                              │TenantMembership │  (User ↔ Tenant join)
                              └─────────────────┘
                              ┌─────────────────┐
                              │ ApplicationUser │  extends IdentityUser<Guid>
                              └─────────────────┘
Domain Enums
text
LeadStatus:        New | Contacted | Qualified | Unqualified | Converted | Lost
DealStatus:        Open | Won | Lost | Abandoned
TaskItemStatus:    Pending | InProgress | Completed | Cancelled
Priority:          Low | Medium | High | Urgent
ActivityType:      Call | Email | Meeting | Note | FollowUp
5. Class Diagram — Application Layer
text
┌─────────────────────┐     ┌──────────────────────┐
│  ICustomerService   │     │  ILeadService        │
├─────────────────────┤     ├──────────────────────┤
│ + GetAllAsync()     │     │ + GetAllAsync()      │
│ + GetByIdAsync()    │     │ + GetByIdAsync()     │
│ + CreateAsync()     │     │ + CreateAsync()      │
│ + UpdateAsync()     │     │ + UpdateAsync()      │
│ + DeleteAsync()     │     │ + DeleteAsync()      │
└─────────────────────┘     └──────────────────────┘

┌─────────────────────┐     ┌──────────────────────┐
│  IDealService       │     │  ITaskService        │
└─────────────────────┘     └──────────────────────┘

┌─────────────────────┐     ┌──────────────────────┐
│  IDashboardService  │     │  ICurrentTenant      │
└─────────────────────┘     └──────────────────────┘

┌─────────────────────┐
│  ICurrentUser       │
└─────────────────────┘
Shared DTOs
PagedResult<T> — Items, TotalCount, Page, PageSize, HasNext, HasPrevious

Result and Result<T> — IsSuccess, Error, Value

6. Class Diagram — Infrastructure Layer
text
┌───────────────────────────────────────────────┐
│           OrbitCRMDbContext                   │
│  : IdentityDbContext<ApplicationUser,         │
│      IdentityRole<Guid>, Guid>                │
├───────────────────────────────────────────────┤
│ + DbSet<Tenant> Tenants                       │
│ + DbSet<TenantMembership> TenantMemberships   │
│ + DbSet<Customer> Customers                   │
│ + DbSet<Lead> Leads                           │
│ + DbSet<PipelineStage> PipelineStages         │
│ + DbSet<Deal> Deals                           │
│ + DbSet<TaskItem> Tasks                       │
│ + DbSet<CrmActivity> Activities               │
├───────────────────────────────────────────────┤
│ OnModelCreating: applies global query filters │
│ SaveChangesAsync: auto-stamps audit + tenant  │
└───────────────────────────────────────────────┘
                       ▲ uses
                       │
┌──────────────────────┴────────────────────────┐
│  ICurrentTenant → CurrentTenant               │
│  ICurrentUser   → CurrentUser                 │
└───────────────────────────────────────────────┘
                       ▲
                       │ implemented by
┌──────────────────────┴────────────────────────┐
│  CustomerService, LeadService, DealService,   │
│  TaskService, DashboardService                │
└───────────────────────────────────────────────┘
7. Entity Relationship Overview
text
                    ┌──────────────┐
                    │    Tenant    │
                    └──────┬───────┘
                           │
        ┌──────────────────┼────────────────────┐
        │                  │                    │
        ▼                  ▼                    ▼
   TenantMembership   Customers            PipelineStages
        │                  │                    │
        ▼                  │                    │
   ApplicationUser         │                    │
                           │                    │
                           ├──► Tasks            │
                           ├──► Activities       │
                           │                     │
                           └──► Deals ◄──────────┘
                                  │
                                  ├──► Tasks
                                  └──► Activities

   Leads ──► Tasks
        ──► Activities
        ──► Deals (on conversion)
Every CRM entity carries TenantId and is filtered globally by EF Core.

8. Feature Breakdown (MVP Scope)
Phase 1 — Foundation ✅ COMPLETE
☑ Solution + projects
☑ Domain entities
☑ DbContext + configurations
☑ Global query filters (tenant isolation)
☑ Dev tenant middleware + seeder
☑ PostgreSQL migrations
☑ CustomerService implementation
☑ Tailwind shell with sidebar
Phase 2 — Core CRM (In Progress)
☑ Customer list page (search, pagination, empty state)
□ Customer create/edit/delete form with validation (Batch 9)
□ Real authentication + login UI (Batch 10)
□ User registration + tenant creation flow (Batch 11)
□ Tenant resolution from auth cookie claims (Batch 12)
Phase 3 — Daily Operations
□ Lead management (list, form, assign, convert to customer)
□ Sales pipeline (Kanban board, drag between stages)
□ Deal management (create, edit, assign)
□ Tasks (list, create, complete, overdue view)
□ Activity timeline on customer/lead pages
Phase 4 — Reporting & Dashboards
□ Dashboard with KPI cards (customers, leads, open deals, revenue)
□ Pipeline value chart
□ Today's activities widget
□ Reports: sales by month, leads by source, conversion rate, per-rep performance
□ Export to Excel/PDF
Phase 5 — Advanced (Post-MVP)
□ Email + WhatsApp integration
□ Invoices and payments
□ Workflow automation
□ Notifications (in-app, email)
□ AI assistance (summaries, email drafts, lead scoring)
□ Audit log UI
□ Multi-role permissions refinement
9. Database Schema Summary
23 tables across three groups:

Tenant & Identity (9):
Tenants, TenantMemberships, AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetRoleClaims, AspNetUserLogins, AspNetUserTokens

CRM Core (8):
Customers, Leads, PipelineStages, Deals, Tasks, Activities

Bookkeeping (1):
__EFMigrationsHistory

(Counts: some CRM tables are counted by plural; the actual DB has more rows.)

10. Security Model
Tenant Isolation — Three Layers
Global query filter on every tenant-owned entity in OnModelCreating:

csharp
builder.Entity<Customer>().HasQueryFilter(e =>
    e.TenantId == _currentTenant.TenantId && !e.IsDeleted);
SaveChangesAsync auto-stamping: New entities automatically get TenantId set from ICurrentTenant. Existing entities cannot have their TenantId changed.

Service-layer checks: Every GetByIdAsync / UpdateAsync / DeleteAsync implicitly restricts to the current tenant via the query filter.

Future (Phase 5): Add PostgreSQL Row-Level Security (RLS) as defense-in-depth.

Authentication
ASP.NET Core Identity with Guid primary keys

ApplicationUser extends IdentityUser<Guid> with FirstName, LastName, IsActive

Cookie-based auth, TenantId claim added on login

Role-based authorization: Platform Admin, Tenant Owner, Sales Manager, Sales Rep, Support Agent

Soft delete
All tenant entities have IsDeleted. Query filters exclude them. Deletion sets the flag rather than physically removing rows.

11. Folder & File Conventions
Domain entities: One class per file, in Entities/.
Enums: One enum per file, in Enums/.
Configurations: One IEntityTypeConfiguration<T> per entity, in Infrastructure/Data/Configurations/.
Services: One class per interface, in Infrastructure/Services/.
DTOs: Grouped by feature in Application/<Feature>/DTOs/.
Blazor pages: One .razor file per route, in Web/Components/Pages/.
Tailwind: Utility classes in markup; scoped .razor.css only for state classes (like .active on NavLink).

Naming: PascalCase everywhere — C# classes, DB tables, properties. PostgreSQL stores them quoted; EF Core + Npgsql handles this.

12. Development Roadmap — Batches
#	Batch	Deliverable	Status
1	Solution setup	5 projects, references, packages	✅
2	Domain entities	All entities + enums	✅
3	Security backbone	DbContext, CurrentTenant, filters	✅
4	Program.cs + DI	Full app wiring	✅
5	First migration	Database tables in PostgreSQL	✅
6	CustomerService + Dev tenant	Seed data, service implementation	✅
7	Tailwind shell	Sidebar, layout, routes	✅
7.5	Tailwind refit	Convert layout to Tailwind v4	✅
8	Customer list page	Search, pagination, empty states	✅
9	Customer form	Create/edit/delete with validation	Next
10	Real authentication	Login, register, logout pages	Planned
11	Tenant onboarding	Register → creates tenant + user	Planned
12	Tenant claim resolution	Read TenantId from auth cookie	Planned
13	Lead management	List + form + convert to customer	Planned
14	Deal management	CRUD + pipeline stage assignment	Planned
15	Pipeline Kanban	Drag-and-drop between stages	Planned
16	Task management	List + create + complete + overdue	Planned
17	Activity timeline	Per-customer event log	Planned
18	Dashboard	KPI cards, pipeline chart, today	Planned
19	Reports	Five core reports	Planned
20	Authorization policies	Permission-based UI + API guards	Planned
21	Audit logging	Track important changes	Planned
22	Production hardening	Backups, HTTPS, secrets, monitoring	Planned
13. Batch 9 Preview — What's Next
Customer create/edit/delete form. Full details:

/customers/new — blank form

/customers/{id} — read-only detail view

/customers/{id}/edit — edit form

Field validation via DataAnnotations ([Required], [EmailAddress], [StringLength])

Duplicate email check within tenant

Save → success toast → redirect to list

Delete → confirmation dialog → soft delete → redirect

All Tailwind-styled

After Batch 9, you have a complete working CRUD module. Batches 13–17 then duplicate the pattern for leads, deals, and tasks with feature-specific twists.

14. Open Decisions
Decision	Options	Recommendation
Multi-tenant model	Shared DB + TenantId / DB-per-tenant	Shared DB (current)
Auth UI source	Scaffolded / Custom	Scaffold + restyle
Tenant resolution	Claim in cookie / Subdomain / Header	Claim in cookie
Email sending	SMTP / SendGrid / Mailgun	TBD when Phase 5 arrives
File storage	Local disk / S3 / Azure Blob	TBD
Deployment target	IIS / Linux + Nginx / Azure / Render	TBD
15. What's Left — Summary
Immediate (Phase 2):

Customer form (create/edit/delete) — Batch 9

Real auth UI — Batches 10–12

Short-term (Phase 3):

Leads module — Batch 13

Deals + Pipeline Kanban — Batches 14–15

Tasks module — Batch 16

Activity timeline — Batch 17

Medium-term (Phase 4):

Dashboard KPI cards — Batch 18

Reports — Batch 19

Post-MVP (Phase 5):

Authorization hardening — Batch 20

Audit logging — Batch 21

Production readiness — Batch 22