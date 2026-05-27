using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualityDMS.Domain.Entities;

namespace QualityDMS.Infrastructure.Persistence.Configurations;

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("WorkflowSteps");
        builder.HasKey(x => x.WorkflowStepId);

        builder.Property(x => x.StepName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.AssignedRoleName).HasMaxLength(100);
        builder.Property(x => x.AssignedUserId).HasMaxLength(450);
        builder.Property(x => x.CreatedBy).HasMaxLength(450);

        builder.HasIndex(x => new { x.WorkflowTemplateId, x.StepOrder }).IsUnique();
    }
}
