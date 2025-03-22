﻿using Microsoft.Extensions.DependencyInjection;
using ProjectName.Application.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ProjectName.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            services.AddScoped<AgentService>();

            return services;
        }
    }
}
