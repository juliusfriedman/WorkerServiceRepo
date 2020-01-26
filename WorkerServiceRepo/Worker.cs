using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace WorkerServiceRepo
{

    public class WorkerOptions
    {   //Fields will not work
        public string Mode { get; set; }
        public int Version { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public SubGroup SubGroup { get; set; }
    }

    public class Option
    {
        public string Name { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public class SubGroup
    {
        public Dictionary<string, Option> OptionDictionary { get; set; }
        public List<string> ListOfValues { get; set; }
    }

    //IHostedService implements BackgroundService
    //https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice
    public class Worker : HostedServiceBase
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerOptions _workerOptions;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="appLifetime"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <param name="services"></param>
        public Worker(IHostApplicationLifetime appLifetime, 
            ILogger<Worker> logger, 
            IOptions<WorkerOptions> settings,
            IServiceProvider services) : base(appLifetime)
        {
            _logger = logger;
            _services = services;
            _workerOptions = settings.Value;
        }

        /// <summary>
        /// Task of worker, starts immediately
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            DateTime sampleBegin, sampleEnd;

            while (false == stoppingToken.IsCancellationRequested)
            {
                int min = 0, max = 0;

                int current = min;

                TaskFactory<int> taskFactory = new TaskFactory<int>();

                HashSet<Task<int>> tasks = new HashSet<Task<int>>();

                int batchSize = 1000;

                _logger.LogInformation("Worker running at: {time} {id}", sampleBegin = DateTime.Now, Thread.CurrentThread.ManagedThreadId);

                using (var db = new DatabaseContext(_workerOptions.Source))
                {
                    max = db.Source.Count();
                }

                //await Repo();

                int wrote = 0;

                Operation:
                tasks.RemoveWhere(t => t.IsCanceled || t.IsCompleted || t.IsFaulted);

                while (current < max && tasks.Count < 10)
                {
                    tasks.Add(taskFactory.StartNew(state =>
                    {
                        int start = (int)state;

                        using (var db = new DatabaseContext(_workerOptions.Destination))
                        {
                            _logger.LogInformation("Repo Chunk {start} Started @ {time} {id}", start,DateTime.Now, Thread.CurrentThread.ManagedThreadId);

                            //var destinations = db.Source.Where(s => s.Id >= start && s.Id < start + batchSize).Select(s => new DestinationNts()
                            //{
                            //    Location = new Point((double)s.Longitude, (double)s.Latitude)
                            //    {
                            //        SRID = 4326
                            //    }
                            //});

                            ////Insert with Entity Framework
                            //db.InsertAll<DestinationNts>(destinations);

                            //////////////////////////

                            var destinations = db.Source.Where(s => s.Id >= start && s.Id < start + batchSize).Select(s => new Destination()
                            {
                                Location = Microsoft.SqlServer.Types.SqlGeography.Point((double)s.Latitude, (double)s.Longitude, 4326)
                            });

                            //Insert via DataTable Conversion (Will not work with NetTopologySuite)
                            db.BulkInsertAll<Destination>(destinations);

                            int count = destinations.Count();

                            wrote += count;

                            _logger.LogInformation("Repo Chunk {start} {count} Complete @ {time} {id}", start, count, DateTime.Now, Thread.CurrentThread.ManagedThreadId);

                            return count;
                        }

                    }, current));

                    current += batchSize;
                }

                await Task.WhenAll(tasks);

                if (current < max) goto Operation;

                _logger.LogInformation("Repo Completed at: {time} {id}", sampleEnd = DateTime.Now, Thread.CurrentThread.ManagedThreadId);

                _logger.LogInformation("Repo Took {time} {id} for {count}", sampleBegin - sampleEnd, Thread.CurrentThread.ManagedThreadId, wrote);

                break;
            }

            await Task.CompletedTask;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called. {id}", System.Threading.Thread.CurrentThread.ManagedThreadId);

            using (var scope = _services.CreateScope())
            {
                IServiceA serviceA = scope.ServiceProvider.GetRequiredService<IServiceA>();
                serviceA.Run();
            }
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called. {id}", System.Threading.Thread.CurrentThread.ManagedThreadId);
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called. {id}", System.Threading.Thread.CurrentThread.ManagedThreadId);
        }

        /// <summary>
        /// The repo code
        /// </summary>
        /// <returns></returns>
        async Task Repo()
        {
            /* 
            --Assumes the following structure with data
            CREATE TABLE [dbo].[Source] (
                [Id]        INT          IDENTITY(1,1),
                [Latitude]  DECIMAL (18, 10) NOT NULL,
                [Longitude] DECIMAL (18, 10) NOT NULL,
                PRIMARY KEY CLUSTERED ([Id] ASC)
            ); GO;

            INSERT [dbo].[Source] ([Id], [Latitude], [Longitude]) VALUES (1, CAST(1.0000000000 AS Decimal(18, 10)), CAST(2.0000000000 AS Decimal(18, 10)))
            GO
            INSERT [dbo].[Source] ([Id], [Latitude], [Longitude]) VALUES (2, CAST(2.0000000000 AS Decimal(18, 10)), CAST(3.0000000000 AS Decimal(18, 10)))
            GO
            INSERT [dbo].[Source] ([Id], [Latitude], [Longitude]) VALUES (3, CAST(3.0000000000 AS Decimal(18, 10)), CAST(4.0000000000 AS Decimal(18, 10)))
            GO
            INSERT [dbo].[Source] ([Id], [Latitude], [Longitude]) VALUES (4, CAST(4.0000000000 AS Decimal(18, 10)), CAST(5.0000000000 AS Decimal(18, 10)))
            GO
            SET IDENTITY_INSERT [dbo].[Source] OFF
            GO

            CREATE TABLE [dbo].[Destination]
            (
            [Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                [Location] [sys].[geography] NULL
            );
             */

            //Todo, could acced db options and pass to base
            //using (DatabaseContext dbContext = new DatabaseContext(_workerOptions.Destination))
            //{
            //    _logger.LogInformation("Success={bool}", dbContext.Database.EnsureCreated());
            //}

            // Open a connection to the LocalDB Test Database
            using (SqlConnection connection = new SqlConnection(_workerOptions.Source))
            {
                await connection.OpenAsync();

                // Perform an initial count on the destination table.
                SqlCommand command = new SqlCommand("SELECT NULL AS Id, geography::Point([Longitude], [Latitude], 4326) AS [Location] FROM [Test].[dbo].[Source]", connection);

                //Create a reader to execute the command to pass to the SqlBulkCopy object.
                using (var reader = await command.ExecuteReaderAsync())
                {
                    // Create the SqlBulkCopy object.
                    // Note that the column positions in the source DataTable
                    // match the column positions in the destination table so
                    // there is no need to map columns.
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(_workerOptions.Destination, SqlBulkCopyOptions.TableLock))
                    {
                        bulkCopy.DestinationTableName = "dbo.Destination";
                        try
                        {
                            // Write unchanged rows from the source to the destination.
                            //use streaming for enhanced performance.
                            bulkCopy.EnableStreaming = true;
                            await bulkCopy.WriteToServerAsync(reader);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                return;
            }
        }
    }
}
