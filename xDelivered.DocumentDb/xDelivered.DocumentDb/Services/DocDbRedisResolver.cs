using System;
using System.Threading.Tasks;
using xDelivered.DocumentDb.Helpers;
using xDelivered.DocumentDb.Interfaces;

namespace xDelivered.DocumentDb.Services
{
    public class DocDbRedisResolver : IObjectResolver
    {
        private readonly IDbContext _dbContext;
        private readonly ICacheProvider _cacheProvider;

        public DocDbRedisResolver(IDbContext dbContext, ICacheProvider cacheProvider)
        {
            _dbContext = dbContext;
            _cacheProvider = cacheProvider;
        }

        public async Task<T> ResolveAsync<T>(string id) where T : IDatabaseModelBase
        {
            return await _cacheProvider.GetOrCreateAsync<T>(
                objectId: CacheHelper.CreateKey<T>(id),
                func: () => _dbContext.GetDocumentAsync<T>(id),
                expiry: TimeSpan.FromDays(7));
        }

        public T Resolve<T>(string id) where T : IDatabaseModelBase
        {
            return _cacheProvider.GetOrCreate<T>(
                key: CacheHelper.CreateKey<T>(id),
                func: () => _dbContext.GetDocument<T>(id),
                expiry: TimeSpan.FromDays(7));
        }
    }
}