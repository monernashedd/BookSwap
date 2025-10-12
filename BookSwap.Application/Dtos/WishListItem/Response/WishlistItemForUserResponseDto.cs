using BookSwap.Core.Enums;

namespace BookSwap.Application.Dtos.WishListItem.Response
{
    public class WishlistItemForUserResponseDto : WishlistItemSharedDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public bool IsApproved { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public WishStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
