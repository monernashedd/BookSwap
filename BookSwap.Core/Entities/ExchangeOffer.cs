using BookSwap.Core.Entities.Identity;
using BookSwap.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSwap.Core.Entities
{
    public class ExchangeOffer
    {
        public int Id { get; set; }
        public int SenderId { get; set; } 
        public int ReceiverId { get; set; } 
        public int RequestedBookId { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ExchangeOfferStatus Status { get; set; } = ExchangeOfferStatus.Pending;
        public int? WishlistItemId { get; set; } 
        [ForeignKey(nameof(SenderId))]
        public User Sender { get; set; } = null!;
        [ForeignKey(nameof(ReceiverId))]
        public User Receiver { get; set; } = null!;
        public Book RequestedBook { get; set; } = null!;
        [ForeignKey(nameof(WishlistItemId))]
        public WishlistItem WishlistItem { get; set; } 
        public IEnumerable<OfferedBook> OfferedBooks { get; set; } // الكتب المعروضة
    }
}
