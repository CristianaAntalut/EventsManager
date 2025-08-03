using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using Amazon.SecretsManager;
using EventsManager.Application.Config;
using EventsManager.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.AwsCloudWatch;

namespace EventsManager.Application;

public partial class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddCors(options =>
         {
             options.AddPolicy("CorsPolicy",
                 builder => builder
                     .AllowAnyOrigin()
                     .AllowAnyMethod()
                     .AllowAnyHeader()
                     .WithExposedHeaders("X-Total-Count"));
         });

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "EventsManager API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
            });
            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
        });

        var awsOptions = new AWSOptions { Region = RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")) };
        services.AddSingleton(awsOptions);
        
        services.AddAWSService<IAmazonCloudWatchLogs>(awsOptions);
        services.AddAWSService<IAmazonS3>(awsOptions);
        services.AddAWSService<IAmazonCognitoIdentityProvider>(awsOptions);

        services.AddAWSService<IAmazonDynamoDB>();
        services.AddSingleton<IDynamoDBContext, DynamoDBContext>(sp =>
        {
            var config = new AmazonDynamoDBConfig
            {
                RegionEndpoint = awsOptions.Region
            };
            var client = new AmazonDynamoDBClient(config);
            return new DynamoDBContext(client);
        });
        services.AddSingleton<IEventAttendeeRepository, EventAttendeeRepository>();


        services.AddAWSService<IAmazonSecretsManager>(awsOptions);
        services.Configure<MyApiCredentials>(Configuration);
        var credentialSettings = Configuration.Get<MyApiCredentials>();
        var bucketConfig = new BucketConfig
        {
            BucketName = credentialSettings.BucketName,
            Prefix = credentialSettings.BucketPrefix,
            Region = awsOptions.Region.SystemName
        };
        var userPoolConfig = new UserPoolConfig
        {
            UserPoolId = credentialSettings.UserPoolId,
            Region = awsOptions.Region.SystemName
        };
        services.AddSingleton(bucketConfig);
        services.AddSingleton(userPoolConfig);

        services.AddAuthorization();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.IncludeErrorDetails = true;
                options.RequireHttpsMetadata = false;
                options.Audience = credentialSettings.AppClientId;
                options.Authority = $"https://cognito-idp.{awsOptions.Region.SystemName}.amazonaws.com/{credentialSettings.UserPoolId}";
                options.MetadataAddress = $"https://cognito-idp.{awsOptions.Region.SystemName}.amazonaws.com/{credentialSettings.UserPoolId}/.well-known/openid-configuration";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = $"https://cognito-idp.{awsOptions.Region.SystemName}.amazonaws.com/{credentialSettings.UserPoolId}",
                    ValidAudience = credentialSettings.AppClientId,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    RoleClaimType = "cognito:groups",
                };
            });

    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        IdentityModelEventSource.ShowPII = true;

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
            });
        });
    }
}