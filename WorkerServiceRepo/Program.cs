using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace WorkerServiceRepo
{
    public class Program
    {
        //https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-3.1&tabs=visual-studio#create-a-service
        //https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-3.1&tabs=visual-studio#start-a-service

        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

            //Supports SC on Windows or installation as a Daemon on Linux
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                hostBuilder.UseWindowsService();
            }
            else
            {
                hostBuilder.UseSystemd();
            }

            hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                    //Configure context
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();
                services.Configure<WorkerOptions>(hostContext.Configuration.GetSection(nameof(WorkerOptions)));
                services.AddScoped<IServiceA, ServiceA>();
                services.AddScoped<IServiceB, ServiceB>();
            });

            return hostBuilder;
        }
    }
}
