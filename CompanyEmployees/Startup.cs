using CompanyEmployees.ActionFilters;
using CompanyEmployees.Extensions;
using CompanyEmployees.Utility;
using Contracts;
using Entities.DTO;
using Entities.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using NLog;
using Repository;
using System.IO;

namespace CompanyEmployees
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            LogManager.LoadConfiguration(string.Concat(Directory.GetCurrentDirectory(),"/nlog.config"));
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers(opt => {
                opt.RespectBrowserAcceptHeader = true;
                opt.ReturnHttpNotAcceptable = true;
                opt.CacheProfiles.Add("120SecondDuration", new CacheProfile { Duration = 120 });
            }).AddNewtonsoftJson()
                .AddXmlDataContractSerializerFormatters()
                .AddCustomCSVFormatter();
            
            services.ConfigureCors();
            services.ConfigureIISIntegration();
            services.ConfigureLoggerService();
            services.ConfigureSqlContext(Configuration);
            services.ConfigureRepositoryManager();
            services.AddAutoMapper(typeof(Startup));

            services.AddScoped<ValidationFilterAttribute>();
            services.AddScoped<ValidateCompanyExistsAttribute>();
            services.AddScoped<ValidateEmployeeForCompanyExistsAttribute>();

            services.AddScoped<IDataShaper<EmployeeDto>, DataShaper<EmployeeDto>>();

            services.AddCustomMediaTypes();
            services.AddScoped<ValidateMediaTypeAttribute>();

            services.AddScoped<EmployeeLinks>();

            services.ConfigureVersioning();

            services.ConfigureResponseCaching();
            services.ConfigureHttpCacheHeaders();

            services.Configure<ApiBehaviorOptions>(opts => opts.SuppressModelStateInvalidFilter = true);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "CompanyEmployees", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerManager logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CompanyEmployees v1"));
            }
            else
            {
                app.UseHsts();
            }

            app.ConfigureExceptionHandler(logger);
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("CorsPolicy");

            app.UseForwardedHeaders(new ForwardedHeadersOptions 
            { 
                ForwardedHeaders = ForwardedHeaders.All 
            });

            app.UseResponseCaching();
            app.UseHttpCacheHeaders();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
