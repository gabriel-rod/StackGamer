using Core.Mapper;
using Database;
using Database.Model;
using Fetcher;
using Fetcher.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using NLog.Extensions.Logging;
using Services;
using Shared;
using Shared.Common;
using Shared.Options;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ScheduledTask
{
    class Program
    {
        private static ILogger<Program> logger;
        private static int TimeBetweenQueries;
        private static int MaxQueriesPerProduct;
        static async Task Main(string[] args)
        {
            try
            {
                var currentTry = 0;
                var productApi = new Fetcher.Model.Thief.Product();
                 
                //https://www.blinkingcaret.com/2018/02/14/net-core-console-logging/

                #region Dependency Injection and Configuration files
                var serviceCollection = new ServiceCollection();
                IConfiguration configuration = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddEnvironmentVariables()
                   .AddCommandLine(args)
                   .Build();
                ConfigureServices(serviceCollection, configuration);
                var serviceProvider = serviceCollection.BuildServiceProvider();

                #endregion

                logger = serviceProvider.GetService<ILogger<Program>>();

                logger.LogInformation($"Starting {Assembly.GetEntryAssembly().GetName().Name}");

                logger.LogInformation("Scrapping data");
                var scraper = serviceProvider.GetService<Scraper>();
                var thief = serviceProvider.GetService<Thief>();
                var parameterService = serviceProvider.GetService<ParametersService>();
                var productService = serviceProvider.GetService<ProductService>();
                TimeBetweenQueries = Convert.ToInt32(parameterService.GetParameter(ParametersKeys.TIME_BETWEEN_QUERIES)?.Value);
                logger.LogInformation("TimeBetweenQueries: " + TimeBetweenQueries);
                MaxQueriesPerProduct = Convert.ToInt32(parameterService.GetParameter(ParametersKeys.MAX_QUERIES_PER_PRODUCT)?.Value);
                logger.LogInformation("MaxQueriesPerProduct: " + MaxQueriesPerProduct);
                if (TimeBetweenQueries == null || MaxQueriesPerProduct == null)
                {
                    logger.LogInformation("No se pudo obtener TimeBetweenQueries o MaxQueriesPerProduct");
                    return;
                }                    

                try
                {
                    var categories = await scraper.GetCategoriesAndProducts();
                    foreach (var category in categories)
                    {
                        logger.LogInformation("Getting product from scrapped category: " + JsonConvert.SerializeObject(category));
                        foreach (var product in category.Products)
                        {
                            logger.LogInformation("product: " + JsonConvert.SerializeObject(product));
                            currentTry = 0;
                            logger.LogInformation("currentTry: " + currentTry);                            
                            while (currentTry < MaxQueriesPerProduct)
                            {
                                productApi = await thief.GetProductById(product.ProductId);
                                logger.LogInformation("productApi: " + JsonConvert.SerializeObject(productApi));
                                if (productApi == null)
                                {
                                    logger.LogInformation(string.Format("No se pudo obtener el producto de la api en la iteración {0}", currentTry + 1));
                                    Thread.Sleep(new TimeSpan(0, 0, TimeBetweenQueries));
                                    currentTry++;
                                    logger.LogInformation("currentTry: " + currentTry);
                                    continue;
                                }
                                productService.InsertUpdateProduct(new Product()
                                {
                                    Name = productApi.Name,
                                    ExternalProductId = product.ProductId,
                                    CategoryId = productApi.CategoryId,
                                    Category = new Category()
                                    {
                                        Name = category.Description,
                                        ExternalIdCategory = category.CategoryId
                                    },
                                    BrandId = productApi.BrandId,
                                    SpecialPrice = productApi.SpecialPrice ?? 0,
                                    ListPrice = productApi.ListPrice ?? 0,
                                    PreviousListPrice = productApi.PreviousListPrice ?? 0,
                                    PreviousSpecialPrice = productApi.PreviousSpecialPrice ?? 0,
                                    Saleable = productApi.Saleable == 1,
                                    Code = productApi.Code
                                });
                                currentTry = MaxQueriesPerProduct;
                                logger.LogInformation("currentTry: " + currentTry);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, e.Message);
                }


               
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }
            finally
            {
                logger.LogInformation($"Exiting {Assembly.GetEntryAssembly().GetName().Name}");
                LogManager.Shutdown();
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<StackGamerOption>(configuration.GetSection("StackGamerOption"));

            var stackGamerOption = new StackGamerOption();
            configuration.Bind("StackGamerOption", stackGamerOption);

            var loggingOption = new LoggingOption();
            configuration.Bind("Logging", loggingOption);

            services
                .AddLogging(logBuilder =>
                {
                    logBuilder.ClearProviders();
                    logBuilder.SetMinimumLevel(loggingOption.LogLevel);
                    logBuilder.AddNLog(configuration);
                })
                .AddMemoryCache()
                .AddTransient<Thief>()
                .AddTransient<StackGameContext>()
                .AddTransient<Scraper>()
                .AddTransient<ProductService>()
                .AddSingleton<ParametersService>()
                .AddHttpClient("stack-gamer", c =>
                {
                    c.BaseAddress = new Uri(stackGamerOption.Urls.BaseUrl);
                    c.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 10) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.185 Mobile Safari/537.36");
                });
        }
    }
}
