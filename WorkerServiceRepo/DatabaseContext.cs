using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace WorkerServiceRepo
{
    public class DatabaseContext : DbContext
    {
        public readonly string ConnectionString;

        public readonly Action<DbContextOptionsBuilder> Configure = (optionsBuilder) => { optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking); };

        public readonly Action<ModelBuilder> CreateModels = (modelBuilder) => { };

        public DbSet<Source> Source { get; set; }

        public DbSet<Destination> Destination { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured) return;
            Configure(optionsBuilder);
            optionsBuilder.UseSqlServer(ConnectionString);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            CreateModels(modelBuilder);
            base.OnModelCreating(modelBuilder);
        }

        public DatabaseContext(string connectionString, Action<DbContextOptionsBuilder> configure = null, Action<ModelBuilder> createModels = null) : base(new DbContextOptions<DatabaseContext>())
        {
            if (null != configure)
            {
                Configure = configure;
            }

            if (null != createModels)
            {
                CreateModels = createModels;
            }

            ConnectionString = connectionString;

            Database.GetDbConnection().ConnectionString = ConnectionString;
        }

        public DatabaseContext(DatabaseContext databaseContext)
            : this(databaseContext.ConnectionString, databaseContext.Configure, databaseContext.CreateModels)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        public void InsertAll<TEntity>(IEnumerable<TEntity> entities, int batchSize = 100) where TEntity : class
        {
            using (TransactionScope scope = new TransactionScope())
            {
                DatabaseContext context = null;

                try
                {
                    context = new DatabaseContext(this);
                    context.ChangeTracker.AutoDetectChangesEnabled = false;

                    int count = 0;
                    foreach (var entityToInsert in entities)
                    {
                        ++count;
                        context = AddToContext(context, entityToInsert, count, batchSize, true);
                    }

                    context.SaveChanges();
                }
                finally
                {
                    if (context != null)
                        context.Dispose();
                }

                scope.Complete();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <param name="count"></param>
        /// <param name="commitCount"></param>
        /// <param name="recreateContext"></param>
        /// <returns></returns>
        DatabaseContext AddToContext<TEntity>(DatabaseContext context, TEntity entity, 
            int count, int commitCount, bool recreateContext) where TEntity : class
        {
            context.Set<TEntity>().Add(entity);

            if (count % commitCount == 0)
            {
                context.SaveChanges();
                if (recreateContext)
                {
                    context.Dispose();
                    context = new DatabaseContext(context);
                    ChangeTracker.AutoDetectChangesEnabled = false;
                }
            }

            return context;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="timeout"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public async Task BulkInsertAllAsync<TEntity>(IEnumerable<TEntity> entities, int timeout = 0, int batchSize = 10000)
            where TEntity : class
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                await conn.OpenAsync();

                using (var bulkCopy = new SqlBulkCopy(conn)
                {
                    DestinationTableName = GetTableName<TEntity>()
                })
                {
                    using (var table = Fill<TEntity>(entities))
                    {
                        bulkCopy.BulkCopyTimeout = timeout;
                        bulkCopy.BatchSize = batchSize;
                        await bulkCopy.WriteToServerAsync(table);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <param name="timeout"></param>
        /// <param name="batchSize"></param>
        public void BulkInsertAll<TEntity>(IEnumerable<TEntity> entities, int timeout = 0, int batchSize = 10000)
            where TEntity : class
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                using (var bulkCopy = new SqlBulkCopy(conn)
                {
                    DestinationTableName = GetTableName<TEntity>()
                })
                {
                    using (var table = Fill(entities))
                    {
                        bulkCopy.BulkCopyTimeout = timeout;
                        bulkCopy.BatchSize = batchSize;
                        bulkCopy.WriteToServer(table);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public string GetTableName<TEntity>() where TEntity : class
        {
            var entityType = Model.FindEntityType(typeof(TEntity));
            string schema = entityType.GetSchema();
            return entityType.GetTableName();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <returns></returns>
        public DataTable Fill<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            var table = new DataTable();

            var properties = typeof(TEntity).GetProperties();

            foreach (var property in properties)
            {
                Type propertyType = property.PropertyType;
                if (propertyType.IsGenericType &&
                    propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                }

                table.Columns.Add(new DataColumn(property.Name, propertyType));
            }

            foreach (var entity in entities)
            {
                table.Rows.Add(properties.Select(property => property.GetValue(entity, null) ?? DBNull.Value).ToArray());
            }

            return table;
        }
    }
}
