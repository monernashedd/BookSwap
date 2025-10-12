using BookSwap.Core.Entities.Identity;
using BookSwap.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSwap.Core.Entities
{
    public class WishlistItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsApproved { get; set; } = false;
        public string RejectionReason { get; set; } = string.Empty;
        public WishStatus Status { get; set; }
        public ExchangeOffer ExchangeOffer { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}
