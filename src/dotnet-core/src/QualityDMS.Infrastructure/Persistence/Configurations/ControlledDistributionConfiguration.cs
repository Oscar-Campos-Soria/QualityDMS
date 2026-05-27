using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualityDMS.Domain.Entities;

namespace QualityDMS.Infrastructure.Persistence.Configurations;

public class ControlledDistributionConfiguration : IEntityTypeConfiguration<ControlledDistribution>
{
    public void Configure(EntityTypeBuilder<ControlledDistribution> builder)
    {
        builder.ToTable("ControlledDistributions");
        builder.HasKey(x => x.DistributionId);

        builder.Property(x => x.RecipientUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.RecipientName).HasMaxLength(200);
        builder.Property(x => x.CreatedBy).HasMaxLength(450);

        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => x.RecipientUserId);
    }
}
