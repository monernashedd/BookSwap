using BookSwap.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace BookSwap.Application.Dtos.WishListItem.Request
{
    public class CreateWishlistItemDto : WishlistItemSharedDto
    {
        public IFormFile Image { get; set; }
        public WishStatus Status { get; set; }

    }
}
