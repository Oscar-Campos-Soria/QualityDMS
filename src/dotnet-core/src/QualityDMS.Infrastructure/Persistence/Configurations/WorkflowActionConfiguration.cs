using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualityDMS.Domain.Entities;
using QualityDMS.Domain.Enums;

namespace QualityDMS.Infrastructure.Persistence.Configurations;

public class WorkflowActionConfiguration : IEntityTypeConfiguration<WorkflowAction>
{
    public void Configure(EntityTypeBuilder<WorkflowAction> builder)
    {
        builder.ToTable("WorkflowActions");
        builder.HasKey(x => x.WorkflowActionId);

        builder.Property(x => x.ActionByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Comments).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(450);

        builder.HasIndex(x => x.WorkflowInstanceId);
        builder.HasIndex(x => x.ActionByUserId);
    }
}
