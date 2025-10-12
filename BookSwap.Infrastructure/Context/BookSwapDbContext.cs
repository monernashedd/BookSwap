using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BookSwap.Core.Entities.Identity;
using BookSwap.Core.Entities;

namespace BookSwap.Infrastructure.Context
{
    public class BookSwapDbContext :  IdentityDbContext<User, Role, int, IdentityUserClaim<int>, IdentityUserRole<int>
                                                         , IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>
   
    {
        public BookSwapDbContext(DbContextOptions options)  : base(options)
        {
            
        }
        public DbSet<User> User { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public DbSet<Role> Role { get; set; } 
        public DbSet<Book> Books { get; set; }
        public DbSet<ExchangeOffer> ExchangeOffers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<OfferedBook> OfferedBooks { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<BookOwnershipHistory> BookOwnershipHistory { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
