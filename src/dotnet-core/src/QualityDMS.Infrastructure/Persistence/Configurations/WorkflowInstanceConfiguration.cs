using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualityDMS.Domain.Entities;
using QualityDMS.Domain.Enums;

namespace QualityDMS.Infrastructure.Persistence.Configurations;

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("WorkflowInstances");
        builder.HasKey(x => x.WorkflowInstanceId);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.CreatedBy).HasMaxLength(450);

        builder.HasIndex(x => new { x.DocumentId, x.Status });

        builder.HasMany(x => x.Actions)
               .WithOne(x => x.WorkflowInstance)
               .HasForeignKey(x => x.WorkflowInstanceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
