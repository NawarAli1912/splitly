using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Splitly.Api.Domain;

namespace Splitly.Api.Database.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description).HasMaxLength(500);

        builder.Property(e => e.Amount)
            .HasConversion(money => money.Cents, cents => Money.FromCents(cents));

        builder.Property(e => e.SplitAmong)
            .HasConversion(
                ids => ids.ToArray(),
                ids => ids.ToList(),
                new ValueComparer<IReadOnlyList<Guid>>(
                    (left, right) => left!.SequenceEqual(right!),
                    ids => ids.Aggregate(0, (hash, id) => HashCode.Combine(hash, id.GetHashCode())),
                    ids => ids.ToList()))
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
