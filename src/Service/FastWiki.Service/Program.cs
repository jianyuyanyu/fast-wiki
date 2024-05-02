using AspNetCoreRateLimit;
using FastWiki.Service;
using FastWiki.Service.Backgrounds;
using FastWiki.Service.Service;
using Masa.Contrib.Authentication.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Serilog;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

var builder = WebApplication.CreateBuilder(args);

// TODO: 由于引用Serilog导致数据库存储失败，暂时注释掉
// Log.Logger = new LoggerConfiguration()
//     .ReadFrom.Configuration(builder.Configuration)
//     .CreateLogger();
//
// builder.Host.UseSerilog();

builder.Configuration.GetSection(OpenAIOption.Name)
    .Get<OpenAIOption>();

builder.Configuration.GetSection(JwtOptions.Name)
    .Get<JwtOptions>();

builder.Configuration.GetSection(ConnectionStringsOptions.Name)
    .Get<ConnectionStringsOptions>();

builder
    .AddLoadEnvironment();

builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimit"));
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

if (ConnectionStringsOptions.DefaultConnection.IsNullOrEmpty())
{
    builder.Services.AddMasaDbContext<SqliteContext>(opt =>
    {
        // 兼容多数据库
        if (ConnectionStringsOptions.DefaultConnection.IsNullOrEmpty())
        {
            // 创建目录data
            if (!Directory.Exists("./data"))
                Directory.CreateDirectory("./data");

            opt.UseSqlite("Data Source=./data/wiki.db");
        }
        else
        {
            opt.UseNpgsql();
        }
    });
}

builder.Services.AddInMemoryRateLimiting()
    .AddScoped<OpenAIService>()
    .AddScoped<FeishuService>()
    .AddScoped<UnitOfWorkMiddleware>()
    .AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            builder => builder
                .SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
    })
    .AddAuthorization()
    .AddHostedService<QuantizeBackgroundService>()
    .AddJwtBearerAuthentication()
    .AddMemoryCache()
    .AddEndpointsApiExplorer()
    .AddMasaIdentity(options =>
    {
        options.UserId = ClaimType.DEFAULT_USER_ID;
        options.Role = "role";
    })
    .AddMapster()
    .AddHttpContextAccessor()
    .AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer xxxxxxxxxxxxxxx\"",
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        });

        options.SwaggerDoc("v1",
            new OpenApiInfo
            {
                Title = "FastWiki.ServiceApp",
                Version = "v1",
                Contact = new OpenApiContact { Name = "FastWiki.ServiceApp", }
            });
        foreach (var item in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.xml"))
            options.IncludeXmlComments(item, true);
        options.DocInclusionPredicate((docName, action) => true);
    })
    .AddMasaDbContext<WikiDbContext>(opt =>
    {
        // 兼容多数据库
        if (ConnectionStringsOptions.DefaultConnection.IsNullOrEmpty())
        {
            // 创建目录data
            if (!Directory.Exists("./data"))
                Directory.CreateDirectory("./data");

            opt.UseSqlite("Data Source=./data/wiki.db");
        }
        else
        {
            opt.UseNpgsql();
        }

        opt.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
    })
    .AddDomainEventBus(dispatcherOptions =>
    {
        dispatcherOptions
            .UseEventBus()
            .UseUoW<WikiDbContext>()
            .UseRepository<WikiDbContext>();
    })
    .AddResponseCompression();

builder.Services.AddAutoInject();

var app = builder.Services.AddServices(builder, option => option.MapHttpMethodsForUnmatched = ["Post"]);

app.Use(async (context, next) =>
{
    var looger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    try
    {
        await next(context);

        if (context.Response.StatusCode == 404)
        {
            context.Request.Path = "/index.html";
            await next(context);
        }
    }
    catch (UserFriendlyException userFriendlyException)
    {
        context.Response.StatusCode = 400;

        looger.LogError(userFriendlyException, userFriendlyException.Message);

        await context.Response.WriteAsJsonAsync(ResultDto.CreateError(userFriendlyException.Message, "400"));
    }
    catch (Exception e)
    {
        context.Response.StatusCode = 500;

        looger.LogError(e, e.Message);

        await context.Response.WriteAsJsonAsync(ResultDto.CreateError(e.Message, "500"));
    }
});

app.UseMiddleware<UnitOfWorkMiddleware>();

var fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider
{
    Mappings =
    {
        [".md"] = "application/octet-stream",
    }
};
app.UseIpRateLimiting();

app.UseResponseCompression();

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = fileExtensionContentTypeProvider
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/v1/chat/completions", (OpenAIService openAIService) => openAIService.Completions)
    .WithTags("OpenAI")
    .WithGroupName("OpenAI")
    .WithDescription("OpenAI Completions")
    .WithOpenApi();

app.MapPost("/v1/feishu/completions/{id}", (FeishuService feishuService) => feishuService.Completions)
    .WithTags("Feishu")
    .WithGroupName("Feishu")
    .WithDescription("飞书对话接入处理")
    .WithOpenApi();

app.MapGet("/api/v1/monaco", (async context =>
{
    // 获取monaco目录下的所有文件
    var files = Directory.GetFiles("monaco", "*.ts");

    var dic = new Dictionary<string, string>();

    foreach (var file in files)
    {
        var info = new FileInfo(file);
        var content = await File.ReadAllTextAsync(file);
        dic.Add(info.Name, content);
    }

    await context.Response.WriteAsJsonAsync(dic);
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger()
        .UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "FastWiki.ServiceApp"));
}


#region MigrationDb

if (ConnectionStringsOptions.DefaultConnection.IsNullOrEmpty())
{
    await using var context = app.Services.CreateScope().ServiceProvider.GetService<SqliteContext>();
    {
        await context!.Database.MigrateAsync();
    }
}
else
{
    await using var context = app.Services.CreateScope().ServiceProvider.GetService<WikiDbContext>();
    {
        await context!.Database.MigrateAsync();

        // TODO: 创建vector插件如果数据库没有则需要提供支持向量的数据库。
        await context.Database.ExecuteSqlInterpolatedAsync($"CREATE EXTENSION IF NOT EXISTS vector;");
    }
}

#endregion

await app.RunAsync();

Log.CloseAndFlush();