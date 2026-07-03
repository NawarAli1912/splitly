using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Splitly.Api.Domain;

namespace Splitly.Api.Database.Configurations;

public sealed class ExpenseGroupConfiguration : IEntityTypeConfiguration<ExpenseGroup>
{
    public void Configure(EntityTypeBuilder<ExpenseGroup> builder)
    {
        builder.ToTable("expense_groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.Name).HasMaxLength(100);

        builder.Property(g => g.Currency).HasMaxLength(3);

        builder.HasMany(g => g.Participants)
            .WithOne()
            .HasForeignKey("ExpenseGroupId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Expenses)
            .WithOne()
            .HasForeignKey("ExpenseGroupId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(g => g.Participants).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(g => g.Expenses).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
