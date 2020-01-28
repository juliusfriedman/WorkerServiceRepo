using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WorkerServiceRepo
{
    public class DatabaseContext : DbContext
    {
        public readonly string ConnectionString;

        public readonly Action<DbContextOptionsBuilder> Configure = (optionsBuilder) => { optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking); };

        public readonly Action<ModelBuilder> CreateModels = (modelBuilder) => { };

        public DbSet<Source> Source { get; set; }

        public DbSet<Destination> Destination { get; set; }

        //public DbSet<DestinationNts> Destinations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured) return;
            Configure(optionsBuilder);
            optionsBuilder.UseSqlServer(ConnectionString, x => x.UseNetTopologySuite());
            optionsBuilder.AddInterceptors(new HintCommandInterceptor());
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
            DatabaseContext context = null;

            context = new DatabaseContext(ConnectionString);
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            int count = 0;
            foreach (var entityToInsert in entities)
            {
                ++count;
                context = AddToContext(context, entityToInsert, count, batchSize, true);
            }

            context.SaveChanges();
            context.Dispose();
            context = null;
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
            using (var bulkCopy = new SqlBulkCopy(ConnectionString, SqlBulkCopyOptions.TableLock)
            {
                DestinationTableName = GetTableName<TEntity>()
            })
            {
                using (var table = Fill(entities))
                {
                    bulkCopy.BulkCopyTimeout = timeout;
                    bulkCopy.BatchSize = batchSize;
                    await bulkCopy.WriteToServerAsync(table);
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
            using (var bulkCopy = new SqlBulkCopy(ConnectionString, SqlBulkCopyOptions.TableLock)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public IDataReader ExecuteDataReader(string sql)
        {
            using (var cmd = Database.GetDbConnection().CreateCommand())
            {
                bool wasOpen = cmd.Connection.State == ConnectionState.Open;
                if (!wasOpen) cmd.Connection.Open();
                try
                {
                    cmd.CommandText = sql;
                    var result = cmd.ExecuteReader();
                    return result;
                }
                finally
                {
                    if (!wasOpen) cmd.Connection.Close();
                }
            }
        }

    }
}

public class HintCommandInterceptor : DbCommandInterceptor
{
    public override DbCommand CommandCreated(CommandEndEventData eventData, DbCommand result)
    {
        return base.CommandCreated(eventData, result);
    }
}

public static class DatabaseContextExtensions
{
    public static string ToSql<TEntity>(this IQueryable<TEntity> query) where TEntity : class
    {
        var enumerator = query.Provider.Execute<IEnumerable<TEntity>>(query.Expression).GetEnumerator();
        var relationalCommandCache = enumerator.Private("_relationalCommandCache");
        var selectExpression = relationalCommandCache.Private<SelectExpression>("_selectExpression");
        var factory = relationalCommandCache.Private<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");

        var sqlGenerator = factory.Create();
        var command = sqlGenerator.GetCommand(selectExpression);

        string sql = command.CommandText;
        return sql;
    }

    private static object Private(this object obj, string privateField) => obj?.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
    private static T Private<T>(this object obj, string privateField) => (T)obj?.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
}
