using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using StudentManagement.Services;
    using System.Text;

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    // 1. Dependency Injection for Data Service
    builder.Services.AddSingleton<SqlDataAccessService>(); 

    // 2. JWT Authentication Setup
    var jwtKey = Encoding.UTF8.GetBytes("ThisIsAVerySecretKeyForJWTAuthToken123456789"); // Must match key in AuthController
    
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "YourAppIssuer",
                ValidAudience = "YourAppAudience",
                IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
            };
        });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    // 3. Add Swagger for API documentation (as required)
    builder.Services.AddSwaggerGen(); 

    // 4. Configure CORS (Cross-Origin Resource Sharing)
    // This is crucial to allow the frontend (running on a different port/origin) to talk to the backend.
// 4. Configure CORS (Cross-Origin Resource Sharing)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend",
            policy =>
            {
                // "AllowAnyOrigin" lets ANYONE connect. 
                // Use this for dev to stop fighting with port numbers.
                policy.AllowAnyOrigin() 
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
    });
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Apply CORS policy
    

    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");
    // Use Authentication and Authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();