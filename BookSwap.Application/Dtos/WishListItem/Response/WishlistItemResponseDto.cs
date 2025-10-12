using BookSwap.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookSwap.Application.Dtos.WishListItem.Response
{
    public class WishlistItemResponseDto : WishlistItemSharedDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty; 
        public WishStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
