namespace BookSwap.Application.Dtos.WishListItem.Request
{
    public class ApproveWishItemRequest
    {
        public int WishItemId { get; set; }
        public bool IsApproved { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }
}