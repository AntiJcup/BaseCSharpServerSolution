using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BaseApi.Services;

namespace BaseApi.Storage
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddMicrosoftSQLDBDataAccessLayer<TDBContext>(this IServiceCollection services) where TDBContext : TemplateMicrosoftSQLDbContext
        {
            services.AddTransient<DBDataLayerInterface, MicrosoftSQLDBDataAccessLayer<TDBContext>>();
            return services.AddTransient<DBDataAccessService>();
        }
    }

    public class MicrosoftSQLDBDataAccessLayer<TDBContext> : DBDataLayerInterface where TDBContext : TemplateMicrosoftSQLDbContext
    {
        private readonly TDBContext dbContext_;

        public MicrosoftSQLDBDataAccessLayer(TDBContext dbContext)
        {
            dbContext_ = dbContext;
        }

        protected Expression<Func<T, bool>> GetKeyLambda<T>(params object[] keys) where T : class, new()
        {
            var modelKeys = dbContext_.Model.FindEntityType(typeof(T)).GetKeys();
            Expression previousExpression = null;
            var keyIndex = 0;
            var param = Expression.Parameter(typeof(T), "entity");

            foreach (var modelKey in modelKeys)
            {
                var property = Expression.Property(param, modelKey.Properties[0].PropertyInfo);
                dynamic val = keys[keyIndex++];
                if (property.Type == typeof(Guid) && val.GetType() == typeof(string))
                {
                    val = Guid.Parse(val as string);
                }
                if (previousExpression == null)
                {
                    previousExpression = Expression.Equal(property, Expression.Constant(val));
                    continue;
                }

                previousExpression = Expression.Or(previousExpression, Expression.Equal(property, Expression.Constant(val)));
            }

            return Expression.Lambda<Func<T, bool>>(previousExpression, param);
        }

        protected Expression<Func<T, bool>> GetIndexLambda<T>(params object[] keys) where T : class, new()
        {
            var modelKeys = dbContext_.Model.FindEntityType(typeof(T)).GetIndexes();
            Expression previousExpression = null;
            var keyIndex = 0;
            var param = Expression.Parameter(typeof(T), "entity");

            foreach (var modelKey in modelKeys)
            {
                var property = Expression.Property(param, modelKey.Properties[0].PropertyInfo);
                dynamic val = keys[keyIndex++];
                if (previousExpression == null)
                {
                    previousExpression = Expression.Equal(property, Expression.Constant(val));
                    continue;
                }

                previousExpression = Expression.Or(previousExpression, Expression.Equal(property, Expression.Constant(val)));
            }

            return Expression.Lambda<Func<T, bool>>(previousExpression, param);
        }

        public async Task<T> Create<T>(T entity) where T : class, new()
        {
            var dbEntity = (await dbContext_.AddAsync(entity)).Entity;
            await dbContext_.SaveChangesAsync();
            dbContext_.Entry(dbEntity).State = EntityState.Detached;
            return dbEntity;
        }

        public async Task Delete<T>(T entity) where T : class, new()
        {
            dbContext_.Remove(entity);
            await dbContext_.SaveChangesAsync();
        }

        public async Task Delete<T>(params object[] keys) where T : class, new()
        {
            await Delete<T>(keys.ToArray() as ICollection<object>);
        }

        public async Task Delete<T>(ICollection<object> keys) where T : class, new()
        {
            var keyProperties = typeof(T).GetProperties().Where(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
            var model = new T();
            var keyOffset = 0;
            foreach (var keyProperty in keyProperties)
            {
                keyProperty.SetValue(model, keys.ElementAt(keyOffset++));
            }
            await Delete(model);
        }

        public async Task<T> Get<T>(params object[] keys) where T : class, new()
        {
            return await Get<T, object>(null, keys);
        }

        public async Task<T> Get<T>(ICollection<object> keys) where T : class, new()
        {
            return await Get<T, object>(null, keys);
        }

        public async Task<T> Get<T, TProperty>(ICollection<Expression<Func<T, TProperty>>> includes, params object[] keys) where T : class, new()
        {
            var dbSet = dbContext_.Set<T>();
            var query = dbSet.AsNoTracking();

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return await query.Where(GetKeyLambda<T>(keys)).AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<T> Get<T, TProperty>(ICollection<Expression<Func<T, TProperty>>> includes, ICollection<object> keys) where T : class, new()
        {
            return await Get<T, TProperty>(includes, keys.ToArray());
        }

        public async Task<ICollection<T>> GetAll<T>(Expression<Func<T, bool>> where = null, int? skip = null, int? take = null) where T : class, new()
        {
            return await GetAll<T, object>(null, where, skip, take);
        }

        public async Task<ICollection<T>> GetAll<T, TProperty>(ICollection<Expression<Func<T, TProperty>>> includes, Expression<Func<T, bool>> where = null, int? skip = null, int? take = null) where T : class, new()
        {
            var dbSet = dbContext_.Set<T>();
            var query = dbSet.AsNoTracking();

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            if (where != null)
            {
                query = query.Where(where);
            }

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.AsNoTracking().ToListAsync();
        }

        public async Task Update<T>(T entity) where T : class, new()
        {
            dbContext_.Update(entity);
            dbContext_.Entry(entity).State = EntityState.Modified;
            await dbContext_.SaveChangesAsync();
            dbContext_.Entry(entity).State = EntityState.Detached;
        }

        public async Task<int> CountAll<T>(Expression<Func<T, bool>> where = null) where T : class, new()
        {
            var dbSet = dbContext_.Set<T>();
            var query = dbSet.AsNoTracking();

            if (where != null)
            {
                query = query.Where(where);
            }

            return await query.AsNoTracking().CountAsync();
        }
    }
}