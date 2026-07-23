using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.WMS.Domain.Entities;
using FluxGrid.Api.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptLine> PurchaseReceiptLines => Set<PurchaseReceiptLine>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<PickList> PickLists => Set<PickList>();
    public DbSet<PickListItem> PickListItems => Set<PickListItem>();
    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<SalaryGrade> SalaryGrades => Set<SalaryGrade>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();

    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateEducation> CandidateEducations => Set<CandidateEducation>();
    public DbSet<CandidateExperience> CandidateExperiences => Set<CandidateExperience>();
    public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
    public DbSet<CandidateDocument> CandidateDocuments => Set<CandidateDocument>();
    public DbSet<CandidateActivityLog> CandidateActivityLogs => Set<CandidateActivityLog>();
    public DbSet<CandidateJobMatch> CandidateJobMatches => Set<CandidateJobMatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.FailedLoginAttempts).HasDefaultValue(0);
            entity.Property(e => e.LockoutEnd);
            entity.Property(e => e.MustChangePassword).HasDefaultValue(false);
            entity.Property(e => e.TenantId).HasDefaultValue(Guid.Empty);

            entity.HasMany(e => e.Roles)
                  .WithMany(e => e.Users)
                  .UsingEntity("UserRoles");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Permissions).HasColumnType("text[]");
        });

        modelBuilder.Entity<ChartOfAccount>(entity =>
        {
            entity.ToTable("chart_of_accounts");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();

            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(20).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Parent)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ResourceType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId });
            entity.HasIndex(e => e.Timestamp);
        });

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.ToTable("journal_entries");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntryNo).IsUnique();
            entity.Property(e => e.EntryNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TransactionDate).IsRequired();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("DRAFT");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasMany(e => e.Lines)
                  .WithOne(e => e.Entry)
                  .HasForeignKey(e => e.EntryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JournalEntryLine>(entity =>
        {
            entity.ToTable("journal_entry_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Debit).HasColumnType("decimal(18,2)").HasDefaultValue(0);
            entity.Property(e => e.Credit).HasColumnType("decimal(18,2)").HasDefaultValue(0);
            entity.HasOne(e => e.Account)
                  .WithMany()
                  .HasForeignKey(e => e.AccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccountingPeriod>(entity =>
        {
            entity.ToTable("accounting_periods");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.StartDate, e.EndDate }).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("OPEN");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable("budgets");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.AccountId, e.PeriodId }).IsUnique();
            entity.Property(e => e.PlannedAmount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Account)
                  .WithMany()
                  .HasForeignKey(e => e.AccountId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Period)
                  .WithMany()
                  .HasForeignKey(e => e.PeriodId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("inventory_items");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Sku }).IsUnique();
            entity.Property(e => e.Sku).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Uom).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<StockLedgerEntry>(entity =>
        {
            entity.ToTable("stock_ledger");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)").IsRequired();
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)").IsRequired();
            entity.Property(e => e.ReferenceType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => new { e.ItemId, e.LocationId }).HasDatabaseName("item_loc_idx");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.TransactionId);
        });

        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.ToTable("inventory_balances");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ItemId, e.LocationId, e.TenantId }).IsUnique();
            entity.Property(e => e.BalanceQty).HasColumnType("decimal(18,4)").IsRequired();
            entity.Property(e => e.BalanceValue).HasColumnType("decimal(18,4)").IsRequired();
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("purchase_orders");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.PoNumber }).IsUnique();
            entity.Property(e => e.PoNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SupplierName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PoDate).IsRequired();
            entity.HasIndex(e => e.TenantId);

            entity.HasMany(e => e.Lines)
                  .WithOne(e => e.Po)
                  .HasForeignKey(e => e.PoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.ToTable("purchase_order_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderedQty).HasColumnType("decimal(18,4)").IsRequired();
            entity.Property(e => e.ReceivedQty).HasColumnType("decimal(18,4)").IsRequired().HasDefaultValue(0);
            entity.HasOne(e => e.Item)
                  .WithMany()
                  .HasForeignKey(e => e.ItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseReceipt>(entity =>
        {
            entity.ToTable("purchase_receipts");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReceiptNo).IsUnique();
            entity.Property(e => e.ReceiptNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PoReference).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.ReceivedBy).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.TenantId);

            entity.HasMany(e => e.Lines)
                  .WithOne(e => e.Receipt)
                  .HasForeignKey(e => e.ReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseReceiptLine>(entity =>
        {
            entity.ToTable("purchase_receipt_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderedQty).HasColumnType("decimal(18,4)").IsRequired();
            entity.Property(e => e.QtyReceived).HasColumnType("decimal(18,4)").IsRequired();
            entity.Property(e => e.QtyPassed).HasColumnType("decimal(18,4)").IsRequired();
            entity.Property(e => e.QtyFailed).HasColumnType("decimal(18,4)").IsRequired();
            entity.HasOne(e => e.Item)
                  .WithMany()
                  .HasForeignKey(e => e.ItemId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PutawayLoc)
                  .WithMany()
                  .HasForeignKey(e => e.PutawayLocId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.ToTable("sales_orders");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.OrderNo }).IsUnique();
            entity.Property(e => e.OrderNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.TenantId);

            entity.HasMany(e => e.Lines)
                  .WithOne(e => e.Order)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SalesOrderLine>(entity =>
        {
            entity.ToTable("sales_order_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QtyOrdered).HasColumnType("decimal(18,4)").IsRequired();
            entity.Property(e => e.QtyReserved).HasColumnType("decimal(18,4)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.QtyPicked).HasColumnType("decimal(18,4)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.QtyShipped).HasColumnType("decimal(18,4)").IsRequired().HasDefaultValue(0);
            entity.HasOne(e => e.Item)
                  .WithMany()
                  .HasForeignKey(e => e.ItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PickList>(entity =>
        {
            entity.ToTable("pick_lists");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.AssignedTo).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.TenantId);

            entity.HasOne(e => e.Order)
                  .WithMany()
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Items)
                  .WithOne(e => e.PickList)
                  .HasForeignKey(e => e.PickListId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PickListItem>(entity =>
        {
            entity.ToTable("pick_list_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QtyExpected).HasColumnType("decimal(18,4)").IsRequired();
            entity.Property(e => e.QtyPicked).HasColumnType("decimal(18,4)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.ShortPickReason).HasMaxLength(200);
            entity.HasOne(e => e.OrderLine)
                  .WithMany()
                  .HasForeignKey(e => e.OrderLineId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Item)
                  .WithMany()
                  .HasForeignKey(e => e.ItemId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Location)
                  .WithMany()
                  .HasForeignKey(e => e.LocationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("shipments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShipmentNo).IsUnique();
            entity.Property(e => e.ShipmentNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.TenantId);

            entity.HasOne(e => e.Order)
                  .WithMany()
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EmployeeNo).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.TenantId);

            entity.Property(e => e.EmployeeNo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Nik).HasMaxLength(30);
            entity.Property(e => e.EmergencyContact).HasMaxLength(200);
            entity.Property(e => e.JobTitle).HasMaxLength(100);
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.BankAccount).HasMaxLength(50);
            entity.Property(e => e.TaxId).HasMaxLength(30);
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("ACTIVE");
            entity.Property(e => e.HireDate).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Manager)
                  .WithMany()
                  .HasForeignKey(e => e.ManagerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Parent)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.ToTable("positions");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.TenantId, e.Title }).IsUnique();

            entity.Property(e => e.Title).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<SalaryGrade>(entity =>
        {
            entity.ToTable("salary_grades");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.TenantId, e.Grade }).IsUnique();

            entity.Property(e => e.Grade).HasMaxLength(50).IsRequired();
            entity.Property(e => e.MinSalary).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.MaxSalary).HasColumnType("decimal(18,2)").IsRequired();
        });

        modelBuilder.Entity<PayrollRun>(entity =>
        {
            entity.ToTable("payroll_runs");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.TenantId, e.PeriodName }).IsUnique();

            entity.Property(e => e.PeriodName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.EndDate).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("DRAFT");
            entity.Property(e => e.TotalGross).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalNet).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ProcessedBy).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasMany(e => e.Records)
                  .WithOne(e => e.Run)
                  .HasForeignKey(e => e.RunId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayrollRecord>(entity =>
        {
            entity.ToTable("payroll_records");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.RunId, e.EmployeeId }).IsUnique();

            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OvertimePay).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LatenessDeduction).HasColumnType("decimal(18,2)");
            entity.Property(e => e.GrossPay).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxDeduction).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NetPay).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.ToTable("candidates");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.TenantId, e.FileHash }).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.LinkedInUrl).HasMaxLength(500);
            entity.Property(e => e.GitHubUrl).HasMaxLength(500);
            entity.Property(e => e.PortfolioUrl).HasMaxLength(500);
            entity.Property(e => e.RawText).HasColumnType("text");
            entity.Property(e => e.Summary).HasMaxLength(2000);
            entity.Property(e => e.TotalExperienceMonths);
            entity.Property(e => e.ExpectedSalaryMin).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ExpectedSalaryMax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NoticePeriodDays);
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("DRAFT");
            entity.Property(e => e.Embedding).HasColumnType("double precision[]");
            entity.Property(e => e.FileUrl).HasMaxLength(1000);
            entity.Property(e => e.FileHash).HasMaxLength(64);
            entity.Property(e => e.OriginalFilename).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.FileSizeBytes);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            entity.HasMany(e => e.Education)
                  .WithOne(e => e.Candidate)
                  .HasForeignKey(e => e.CandidateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Experience)
                  .WithOne(e => e.Candidate)
                  .HasForeignKey(e => e.CandidateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Skills)
                  .WithOne(e => e.Candidate)
                  .HasForeignKey(e => e.CandidateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Documents)
                  .WithOne(e => e.Candidate)
                  .HasForeignKey(e => e.CandidateId)
                  .OnDelete(DeleteBehavior.Cascade);

        });

        modelBuilder.Entity<JobPosting>(entity =>
        {
            entity.ToTable("job_postings");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnType("text").IsRequired();
            entity.Property(e => e.Requirements).HasColumnType("text");
            entity.Property(e => e.RequiredSkills).HasColumnType("text[]");
            entity.Property(e => e.MinExperienceYears);
            entity.Property(e => e.MaxExperienceYears);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.SalaryMin).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SalaryMax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("DRAFT");
            entity.Property(e => e.Embedding).HasColumnType("double precision[]");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<CandidateEducation>(entity =>
        {
            entity.ToTable("candidate_education");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Institution).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Degree).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FieldOfStudy).HasMaxLength(200);
            entity.Property(e => e.Gpa).HasColumnType("decimal(3,2)");
        });

        modelBuilder.Entity<CandidateExperience>(entity =>
        {
            entity.ToTable("candidate_experience");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Company).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Location).HasMaxLength(200);
        });

        modelBuilder.Entity<CandidateSkill>(entity =>
        {
            entity.ToTable("candidate_skills");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.SkillName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SkillCategory).HasMaxLength(100);
            entity.Property(e => e.ProficiencyLevel).HasMaxLength(50);
        });

        modelBuilder.Entity<CandidateDocument>(entity =>
        {
            entity.ToTable("candidate_documents");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.FileUrl).HasMaxLength(1000);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<CandidateActivityLog>(entity =>
        {
            entity.ToTable("candidate_activity_logs");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.CandidateId, e.CreatedAt });

            entity.Property(e => e.Action).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Details).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Candidate)
                  .WithMany()
                  .HasForeignKey(e => e.CandidateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateJobMatch>(entity =>
        {
            entity.ToTable("candidate_job_matches");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.CandidateId, e.JobId }).IsUnique();

            entity.Property(e => e.Score).HasDefaultValue(1.0);
            entity.Property(e => e.IsManual).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Candidate)
                  .WithMany()
                  .HasForeignKey(e => e.CandidateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.JobPosting)
                  .WithMany()
                  .HasForeignKey(e => e.JobId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
