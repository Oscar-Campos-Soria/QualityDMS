using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualityDMS.Domain.Entities;

namespace QualityDMS.Infrastructure.Persistence.Configurations;

public class WorkflowTemplateConfiguration : IEntityTypeConfiguration<WorkflowTemplate>
{
    public void Configure(EntityTypeBuilder<WorkflowTemplate> builder)
    {
        builder.ToTable("WorkflowTemplates");
        builder.HasKey(x => x.WorkflowTemplateId);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).HasMaxLength(450);

        builder.HasMany(x => x.Steps)
               .WithOne(x => x.WorkflowTemplate)
               .HasForeignKey(x => x.WorkflowTemplateId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
