using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogAPI.CustomFormatter;
using CatalogAPI.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;

namespace CatalogAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<CatalogContext>();
            services.AddCors(c =>
            {
                c.AddDefaultPolicy(x => x.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

                c.AddPolicy("AllowPartners", x =>
                 {
                     x.WithOrigins("http://microsoft.com", "https://synergetics.com")
                     .WithMethods("GET", "POST")
                     .AllowAnyHeader();
                 });

                c.AddPolicy("AllowAll", x =>
                {
                    x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });
            services.AddSwaggerGen(options => {
                options.SwaggerDoc("v1", new Info
                {
                    Title = "Catalog API",
                    Description = "Catalog management API methods for Eshop application",
                    Version = "1.0",
                    Contact = new Contact
                    {
                        Name = "Karthika Rajaratna",
                        Email = "karthiratnas@gmail.com",
                        Url = "https://github.com/KarthiRatna"
                    }
                });
            });
            services.AddAuthentication(c =>
            {
                c.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                //trying to access the page that is not allowed- it will go to default page
                c.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(c =>
            {
                c.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration.GetValue<string>("jwt:issuer"),
                    ValidAudience = Configuration.GetValue<string>("jwt:audience"),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetValue<string>("jwt:secret")))
                };
            });
            services.AddMvc(options=>
            {
                options.OutputFormatters.Add(new CsvOutputFormatter());
            })
            .AddXmlDataContractSerializerFormatters().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwaggerUI(config =>
                {
                    config.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API");
                    config.RoutePrefix = "";
                });
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseSwagger();  //https://localhost:5001/swagger/v1/swagger.json
            //app.UseFileServer(new FileServerOptions() { 
            //                RequestPath = "Images",
            //                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Images")),
            //                EnableDirectoryBrowsing=true
            //                });

            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
