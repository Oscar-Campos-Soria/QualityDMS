using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualityDMS.Domain.Entities;

namespace QualityDMS.Infrastructure.Persistence.Configurations;

public class AuditFindingConfiguration : IEntityTypeConfiguration<AuditFinding>
{
    public void Configure(EntityTypeBuilder<AuditFinding> builder)
    {
        builder.ToTable("AuditFindings");
        builder.HasKey(x => x.FindingId);

        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.FindingType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CorrectiveAction).HasMaxLength(2000);
        builder.Property(x => x.CreatedBy).HasMaxLength(450);

        builder.HasIndex(x => x.AuditId);
        builder.HasIndex(x => x.IsClosed);
    }
}
