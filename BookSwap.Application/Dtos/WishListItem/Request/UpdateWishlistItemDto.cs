using Microsoft.AspNetCore.Http;

namespace BookSwap.Application.Dtos.WishListItem.Request
{
    public class UpdateWishlistItemDto : WishlistItemSharedDto
    {
        public IFormFile? Image { get; set; }

    }
}
