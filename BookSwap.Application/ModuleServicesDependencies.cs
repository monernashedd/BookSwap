

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BookSwap.Core.Helping;
using BookSwap.Application.Abstracts;
using BookSwapImplementations;
using BookSwap.Application.Implementations;
using BookSwap.Application.Implemetations;
using BookExchange.Infrastructure.Services;
namespace BookSwap.Application
{
    public static class ModuleApiServicesDependencies
    {
        public static IServiceCollection AddServicesDependencies(this IServiceCollection services, IConfiguration configuration)
        {      
            services.AddTransient<ICurrentUserService, CurrentUserService>();
            services.AddTransient<IApplicationUserService, ApplicationUserService>();
            services.AddTransient<IAuthonticationService, AuthonticationService>();
            services.AddTransient<IMediaService, MediaService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IBookService, BookService>();
            services.AddTransient<IExchangeOfferService, ExchangeOfferService>();
            services.AddTransient<IWishlistService, WishlistService>();


            //BackgroundServices
            services.AddHostedService<RefreshTokenCleanupService>();

            //Email Setting
            var emailSettings = new EmailSettings();
            configuration.GetSection(nameof(emailSettings)).Bind(emailSettings);
            services.AddSingleton(emailSettings);

            //JWT Authentication
            var jwtSettings = new JwtSettings();
            configuration.GetSection(nameof(jwtSettings)).Bind(jwtSettings);
            services.AddSingleton(jwtSettings);

       


            return services;
        }
    }
}
