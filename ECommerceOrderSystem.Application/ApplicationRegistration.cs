using ECommerceOrderSystem.Application.Services;
using ECommerceOrderSystem.Application.Services.Interface;
using ECommerceOrderSystem.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerceOrderSystem.Application
{
    public static class ApplicationRegistration
    {
        public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IOrderLifecycleService, OrderLifecycleService>();
            services.AddScoped<IOrderService, OrderService>();
            return services;
        }
    }
}

