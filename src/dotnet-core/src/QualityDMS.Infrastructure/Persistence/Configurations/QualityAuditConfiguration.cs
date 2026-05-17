using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualityDMS.Domain.Entities;

namespace QualityDMS.Infrastructure.Persistence.Configurations;

public class QualityAuditConfiguration : IEntityTypeConfiguration<QualityAudit>
{
    public void Configure(EntityTypeBuilder<QualityAudit> builder)
    {
        builder.ToTable("QualityAudits");
        builder.HasKey(x => x.AuditId);

        builder.Property(x => x.AuditCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.AuditorUserId).HasMaxLength(450);
        builder.Property(x => x.Summary).HasMaxLength(4000);
        builder.Property(x => x.CreatedBy).HasMaxLength(450);

        builder.HasIndex(x => x.AuditCode).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Findings)
               .WithOne(x => x.Audit)
               .HasForeignKey(x => x.AuditId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
