using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSwap.Core.Entities.Identity
{
    public class User : IdentityUser<int>
    {
        
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int LoyaltyPoints { get; set; } = 0;
        public string ImageUrl { get; set; }
        public string? ResetPasswordCode { get; set; }
        public DateTime? ResetPasswordCodeExpiry { get; set; }
        public bool IsBanned { get; set; } = false;  
        public DateTime CreatedAt { get; set; } = DateTime.Now;  
        public DateTime? UpdatedAt { get; set; }  
        [InverseProperty(nameof(UserRefreshToken.User))]
        public virtual ICollection<UserRefreshToken>? UserRefreshTokens { get; set; }      
   
        [InverseProperty(nameof(Book.Owner))]
        public ICollection<Book> OwnedBooks { get; set; }
        [InverseProperty(nameof(ExchangeOffer.Sender))]
        public ICollection<ExchangeOffer> SentOffers { get; set; } =new List<ExchangeOffer>();
        [InverseProperty(nameof(ExchangeOffer.Receiver))]
        public ICollection<ExchangeOffer> ReceivedOffers { get; set; } = new List<ExchangeOffer>();
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

    }
}
