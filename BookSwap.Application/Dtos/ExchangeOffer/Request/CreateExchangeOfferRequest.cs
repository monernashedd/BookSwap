using System.ComponentModel.DataAnnotations;
namespace BookSwap.Application.Dtos.ExchangeOffer.Request
{
    public class CreateExchangeOfferRequest
    {
        [Required]
        public int RequestedBookId { get; set; } // الكتاب المطلوب
        public int? WishlistItemId { get; set; } // الكتاب المطلوب
        [Required]
        public List<int> OfferedBookIds { get; set; } = new List<int>(); // الكتب المعروضة
    }
}
