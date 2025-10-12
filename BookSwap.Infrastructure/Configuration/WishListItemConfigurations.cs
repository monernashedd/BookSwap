using BookSwap.Core.Entities;
using BookSwap.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSwap.Infrastructure.Configuration
{
    public class WishListItemConfigurations : IEntityTypeConfiguration<WishlistItem>
    {
        public void Configure(EntityTypeBuilder<WishlistItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                   .HasColumnType("nvarchar")
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Author)
                   .HasColumnType("nvarchar")
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Description)
                   .HasColumnType("nvarchar")
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(x => x.ImageUrl)
                   .HasColumnType("nvarchar")
                   .HasMaxLength(400)
                   .IsRequired();

            builder.Property(b => b.Status)
                   .HasColumnType("int")
                   .IsRequired()
                   .HasDefaultValue(WishStatus.Private);

            builder.Property(b => b.IsApproved)
                   .HasColumnType("bit")
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(x => x.CreatedAt)
                   .HasColumnType("datetime2")
                   .IsRequired();

            builder.Property(b => b.RejectionReason)
                 .HasColumnType("nvarchar")
                 .HasMaxLength(500)
                 .HasDefaultValue(string.Empty);

            builder.HasOne(x => x.User)
                   .WithMany(x => x.WishlistItems)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

}


