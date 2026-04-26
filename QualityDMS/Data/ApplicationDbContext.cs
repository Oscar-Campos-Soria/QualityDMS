using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QualityDMS.Models;

namespace QualityDMS.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Department> Departments { get; set; }
    public DbSet<DocumentCategory> DocumentCategories { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentVersion> DocumentVersions { get; set; }
    public DbSet<WorkflowTemplate> WorkflowTemplates { get; set; }
    public DbSet<WorkflowTemplateStep> WorkflowTemplateSteps { get; set; }
    public DbSet<WorkflowInstance> WorkflowInstances { get; set; }
    public DbSet<WorkflowAction> WorkflowActions { get; set; }
    public DbSet<QualityAudit> QualityAudits { get; set; }
    public DbSet<AuditFinding> AuditFindings { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── ApplicationUser ──────────────────────────────────
        builder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("AspNetUsers");
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.Property(u => u.Position).HasMaxLength(150);
            e.HasOne(u => u.Department)
             .WithMany(d => d.Users)
             .HasForeignKey(u => u.DepartmentId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Department ───────────────────────────────────────
        builder.Entity<Department>(e =>
        {
            e.HasKey(d => d.DepartmentId);
            e.HasIndex(d => d.Code).IsUnique();
            e.Property(d => d.Code).HasMaxLength(20).IsRequired();
            e.Property(d => d.Name).HasMaxLength(150).IsRequired();
        });

        // ── DocumentCategory ─────────────────────────────────
        builder.Entity<DocumentCategory>(e =>
        {
            e.HasKey(c => c.CategoryId);
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Code).HasMaxLength(20).IsRequired();
            e.HasOne(c => c.Parent)
             .WithMany(c => c.Children)
             .HasForeignKey(c => c.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Document ─────────────────────────────────────────
        builder.Entity<Document>(e =>
        {
            e.HasKey(d => d.DocumentId);
            e.HasIndex(d => d.Code).IsUnique();
            e.Property(d => d.Code).HasMaxLength(50).IsRequired();
            e.Property(d => d.CurrentStatus).HasConversion<byte>();

            e.HasOne(d => d.Owner)
             .WithMany(u => u.OwnedDocuments)
             .HasForeignKey(d => d.OwnerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(d => d.Category)
             .WithMany(c => c.Documents)
             .HasForeignKey(d => d.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(d => d.Department)
             .WithMany(dep => dep.Documents)
             .HasForeignKey(d => d.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(d => d.CurrentVersionNav)
             .WithMany()
             .HasForeignKey(d => d.CurrentVersionId)
             .OnDelete(DeleteBehavior.NoAction); // ← fix

            e.HasIndex(d => new { d.CurrentStatus, d.IsConfidential });
            e.HasIndex(d => d.DepartmentId);
            e.HasIndex(d => d.OwnerId);
        });

        // ── DocumentVersion ──────────────────────────────────
        builder.Entity<DocumentVersion>(e =>
        {
            e.HasKey(v => v.VersionId);
            e.HasIndex(v => new { v.DocumentId, v.VersionNumber }).IsUnique();
            e.Property(v => v.VersionType).HasConversion<byte>();
            e.Property(v => v.Status).HasConversion<byte>();

            e.HasOne(v => v.Document)
             .WithMany(d => d.Versions)
             .HasForeignKey(v => v.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(v => v.Author)
             .WithMany(u => u.AuthoredVersions)
             .HasForeignKey(v => v.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(v => v.ReviewedBy)
             .WithMany()
             .HasForeignKey(v => v.ReviewedById)
             .OnDelete(DeleteBehavior.NoAction); // ← fix

            e.HasOne(v => v.ApprovedBy)
             .WithMany()
             .HasForeignKey(v => v.ApprovedById)
             .OnDelete(DeleteBehavior.NoAction); // ← fix

            e.HasIndex(v => new { v.DocumentId, v.Status });
        });

        // ── WorkflowTemplateStep ─────────────────────────────
        builder.Entity<WorkflowTemplateStep>(e =>
        {
            e.HasKey(s => s.StepId);
            e.HasIndex(s => new { s.TemplateId, s.StepOrder }).IsUnique();
            e.Property(s => s.StepType).HasConversion<byte>();

            e.HasOne(s => s.Template)
             .WithMany(t => t.Steps)
             .HasForeignKey(s => s.TemplateId)
             .OnDelete(DeleteBehavior.NoAction); // ← fix

            e.HasOne(s => s.Assignee)
             .WithMany()
             .HasForeignKey(s => s.AssigneeId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── WorkflowInstance ─────────────────────────────────
        builder.Entity<WorkflowInstance>(e =>
        {
            e.HasKey(w => w.InstanceId);
            e.Property(w => w.Status).HasConversion<byte>();
            e.HasIndex(w => new { w.VersionId, w.Status });

            e.HasOne(w => w.InitiatedByUser)
             .WithMany()
             .HasForeignKey(w => w.InitiatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── WorkflowAction ───────────────────────────────────
        builder.Entity<WorkflowAction>(e =>
        {
            e.HasKey(a => a.ActionId);
            e.Property(a => a.ActionType).HasConversion<byte>();
            e.Property(a => a.StepType).HasConversion<byte>();

            e.HasOne(a => a.Actor)
             .WithMany()
             .HasForeignKey(a => a.ActorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── QualityAudit ─────────────────────────────────────
        builder.Entity<QualityAudit>(e =>
        {
            e.HasKey(a => a.AuditId);
            e.HasIndex(a => a.Code).IsUnique();
            e.Property(a => a.AuditType).HasConversion<byte>();

            e.HasOne(a => a.LeadAuditor)
             .WithMany()
             .HasForeignKey(a => a.LeadAuditorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.CreatedByUser)
             .WithMany()
             .HasForeignKey(a => a.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AuditFinding ─────────────────────────────────────
        builder.Entity<AuditFinding>(e =>
        {
            e.HasKey(f => f.FindingId);
            e.Property(f => f.FindingType).HasConversion<byte>();
            e.Property(f => f.Status).HasConversion<byte>();

            e.HasOne(f => f.Responsible)
             .WithMany()
             .HasForeignKey(f => f.ResponsibleId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(f => f.VerifiedBy)
             .WithMany()
             .HasForeignKey(f => f.VerifiedById)
             .OnDelete(DeleteBehavior.NoAction); // ← fix
        });

        // ── AuditLog ─────────────────────────────────────────
        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(l => l.LogId);
            e.HasIndex(l => new { l.EntityType, l.EntityId });
            e.HasIndex(l => l.ChangedAt);

            e.HasOne(l => l.User)
             .WithMany()
             .HasForeignKey(l => l.ChangedBy)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Notification ─────────────────────────────────────
        builder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.NotificationId);
            e.HasIndex(n => new { n.UserId, n.IsRead });
        });
    }
}