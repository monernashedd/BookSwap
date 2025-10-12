using BookSwap.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSwap.Infrastructure.Configuration
{
    public class ExchangeOfferConfigurations : IEntityTypeConfiguration<ExchangeOffer>
    {
        public void Configure(EntityTypeBuilder<ExchangeOffer> builder)
        {
            builder.HasKey(ob => ob.Id);

            builder.Property(x => x.CreatedAt)
                   .HasColumnType("datetime2")
                   .IsRequired();

            builder.Property(x => x.Status)
                  .HasConversion<int>()
                  .HasComment("Represents Exchange offer status: 0 = Pending, 1 = Accepted, 2 = Rejected , 3 =Cancelled ")
                  .IsRequired(true);

            builder.HasOne(ob => ob.Sender)
                   .WithMany(x=>x.SentOffers)
                   .HasForeignKey(ob => ob.SenderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ob => ob.Receiver)
                   .WithMany(x => x.ReceivedOffers)
                   .HasForeignKey(ob => ob.ReceiverId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ob => ob.RequestedBook)
                  .WithMany(x => x.ExchangeOffers)
                  .HasForeignKey(ob => ob.RequestedBookId)
                  .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ob => ob.WishlistItem)
                .WithOne(x => x.ExchangeOffer)
                .HasForeignKey<ExchangeOffer>(ob => ob.WishlistItemId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}


