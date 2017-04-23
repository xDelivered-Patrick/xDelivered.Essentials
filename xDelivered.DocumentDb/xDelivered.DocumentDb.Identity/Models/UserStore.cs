using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using xDelivered.DocumentDb.Helpers;
using xDelivered.DocumentDb.Interfaces;

namespace xDelivered.DocumentDb.Identity.Models
{
    public class UserStore<TUser> : IUserLoginStore<TUser>, IUserClaimStore<TUser>, IUserRoleStore<TUser>, IUserPasswordStore<TUser>, 
        IUserSecurityStampStore<TUser>, IUserStore<TUser>, IUserEmailStore<TUser>, IUserLockoutStore<TUser, string>, 
        IUserTwoFactorStore<TUser, string>, IUserPhoneNumberStore<TUser>, IQueryableUserStore<TUser, String>
        where TUser : IdentityUser
    {
        private readonly ICacheProvider _cacheProvider;

        private readonly string _database;
        private readonly string _collection;
        private readonly Uri _documentCollection;

        private readonly DocumentClient _client;
        private static DocumentClient _cachedClient;

        private Dictionary<string, TUser> _userMemoryCache = new Dictionary<string, TUser>();

        public UserStore(Uri endPoint, string authKey, string database, string collection, ICacheProvider cacheProvider, bool ensureDatabaseAndCollection = false) : this(GetOrCreateClient(endPoint, authKey), database, collection, ensureDatabaseAndCollection)
        {
            _cacheProvider = cacheProvider;
        }

        public UserStore(DocumentClient client, string database, string collection, bool ensureDatabaseAndCollection = false)
        {
            if (client == null)
            {
                throw new ArgumentException("client");
            }
            _client = client;

            if (string.IsNullOrEmpty(database))
            {
                throw new ArgumentException("database");
            }
            _database = database;

            if (string.IsNullOrEmpty(collection))
            {
                throw new ArgumentException("collection");
            }
            _collection = collection;

            if (ensureDatabaseAndCollection)
            {
                Task.Run(async () =>
                {
                    await CreateDatabaseIfNotExistsAsync();
                    await CreateCollectionIfNotExistsAsync();
                }).Wait();
            }

            _documentCollection = UriFactory.CreateDocumentCollectionUri(_database, _collection);
        }

        private static DocumentClient GetOrCreateClient(Uri endPoint, string authKey)
        {
            return _cachedClient ?? (_cachedClient = new DocumentClient(endPoint, authKey));
        }

        private async Task CreateDatabaseIfNotExistsAsync()
        {
            bool databaseEnsured;

            try
            {
                await _client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(_database));
                databaseEnsured = true;
            }
            catch (DocumentClientException exception)
            {
                if (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    databaseEnsured = false;
                }
                else
                {
                    throw;
                }
            }

            if (!databaseEnsured)
            {
                await _client.CreateDatabaseAsync(new Database {Id = _database});
            }
        }

        private async Task CreateCollectionIfNotExistsAsync()
        {
            bool collectionEnsured;

            try
            {
                await _client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(_database, _collection));
                collectionEnsured = true;
            }
            catch (DocumentClientException exception)
            {
                if (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    collectionEnsured = false;
                }
                else
                {
                    throw;
                }
            }

            if (!collectionEnsured)
            {
                await _client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(_database),
                        new DocumentCollection { Id = _collection },
                        new RequestOptions { OfferThroughput = 400 });
            }
        }

        public async Task<IEnumerable<TUser>> GetUsers(Expression<Func<TUser, bool>> predicate)
        {
            var query = _client.CreateDocumentQuery<TUser>(_documentCollection)
                .Where(predicate)
                .AsDocumentQuery();

            var results = new List<TUser>();
            while (query.HasMoreResults)
            {
                try
                {
                    results.AddRange(await query.ExecuteNextAsync<TUser>());
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
            }

            return results;
        }

        public async Task AddLoginAsync(TUser user, UserLoginInfo login)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (login == null)
            {
                throw new ArgumentNullException("login");
            }

            if (!user.Logins.Any(x => x.LoginProvider == login.LoginProvider && x.ProviderKey == login.ProviderKey))
            {
                user.Logins.Add(login);
            }

            await UpdateUserAsync(user);
        }

        public async Task<TUser> FindAsync(UserLoginInfo login)
        {
            ThrowIfDisposed();

            if (login == null)
            {
                throw new ArgumentNullException("login");
            }

            return (from user in await GetUsers(user => user.Logins != null)
                    from userLogin in user.Logins
                    where userLogin.LoginProvider == login.LoginProvider && userLogin.ProviderKey == userLogin.ProviderKey
                    select user).FirstOrDefault();
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.Logins.ToIList());
        }

        public Task RemoveLoginAsync(TUser user, UserLoginInfo login)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (login == null)
            {
                throw new ArgumentNullException("login");
            }

            user.Logins.Remove(u => u.LoginProvider == login.LoginProvider && u.ProviderKey == login.ProviderKey);

            return Task.FromResult(0);
        }

        public async Task CreateAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (string.IsNullOrEmpty(user.Id))
            {
                user.Id = Guid.NewGuid().ToString();
            }
            
            await _cacheProvider.SetObject(CacheHelper.CreateKey(user, x=>x.Id), user);
            await _client.CreateDocumentAsync(_documentCollection, user);
        }

        public async Task DeleteAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var doc = _client.CreateDocumentQuery(_documentCollection).FirstOrDefault(u => u.Id == user.Id);
            if (doc != null)
            {
                await _client.DeleteDocumentAsync(doc.SelfLink);
            }
        }

        public async Task<TUser> FindByIdAsync(string userId)
        {
            ThrowIfDisposed();

            if (userId == null)
            {
                throw new ArgumentNullException("userId");
            }

            if (_userMemoryCache.ContainsKey(userId) && _userMemoryCache[userId] != null)
            {
                return _userMemoryCache[userId];
            }

            //todo : allow to turn and off memory cache
            var result =  await _cacheProvider.GetOrCreateAsync<TUser>(CacheHelper.CreateKey<TUser>(userId), async () =>
            {
                return (await GetUsers(user => user.Id == userId)).FirstOrDefault();
            }) as TUser;

            if (result != null)
            {
                _userMemoryCache[userId] = result;
            }
            
            return result;
        }

        public async Task<TUser> FindByNameAsync(string userName)
        {
            ThrowIfDisposed();

            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }

            if (_userMemoryCache.ContainsKey(userName) && _userMemoryCache[userName] != null)
            {
                return _userMemoryCache[userName];
            }

            TUser result = await _cacheProvider.GetOrCreateAsync<TUser>(CacheHelper.CreateKey<TUser>(userName), async () =>
            {
                return (await GetUsers(user => user.UserName == userName)).FirstOrDefault();
            });

            if (result != null)
            {
                _userMemoryCache[userName] = result;
            }

            return result;
        }

        public async Task UpdateAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            await UpdateUserAsync(user);
        }

        public Task AddClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (!user.Claims.Any(x => x.ClaimType == claim.Type && x.ClaimValue == claim.Value))
            {
                user.Claims.Add(new IdentityUserClaim
                {
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }

            return Task.FromResult(0);
        }

        public Task<IList<Claim>> GetClaimsAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            IList<Claim> result = user.Claims.Where(x=>x.ClaimValue != null).Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList();
            return Task.FromResult(result);
        }

        public Task RemoveClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.Claims.RemoveAll(x => x.ClaimType == claim.Type && x.ClaimValue == claim.Value);
            return Task.FromResult(0);
        }

        public Task AddToRoleAsync(TUser user, string roleName)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (roleName == null)
            {
                throw new ArgumentNullException("roleName");
            }

            if (!user.Roles.Any(x => x.Equals(roleName)))
            {
                user.Roles.Add(roleName);
            }

            return Task.FromResult(0);
        }

        public Task<IList<string>> GetRolesAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var result = user.Roles.ToIList();

            return Task.FromResult(result);
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (roleName == null)
            {
                throw new ArgumentNullException("roleName");
            }

            var isInRole = user.Roles.Any(x => x.Equals(roleName));

            return Task.FromResult(isInRole);
        }

        public Task RemoveFromRoleAsync(TUser user, string roleName)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (roleName == null)
            {
                throw new ArgumentNullException("roleName");
            }

            user.Roles.Remove(x => x.Equals(roleName));

            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.PasswordHash != null);
        }

        public async Task SetPasswordHashAsync(TUser user, string passwordHash)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.PasswordHash = passwordHash;
        }

        public Task<string> GetSecurityStampAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.SecurityStamp);
        }

        public async Task SetSecurityStampAsync(TUser user, string stamp)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.SecurityStamp = stamp;
        }

        public async Task<TUser> FindByEmailAsync(string email)
        {
            ThrowIfDisposed();

            if (email == null)
            {
                throw new ArgumentNullException("email");
            }
            
            return await _cacheProvider.GetOrCreateAsync<TUser>(CacheHelper.CreateKey<TUser>(email), async () =>
            {
                return (await GetUsers(user => user.Email == email)).FirstOrDefault();
            });
        }

        public Task<string> GetEmailAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailAsync(TUser user, string email)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (email == null)
            {
                throw new ArgumentNullException("email");
            }

            user.Email = email;

            return Task.FromResult(0);
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.EmailConfirmed = confirmed;

            return Task.FromResult(0);
        }

        public Task<int> GetAccessFailedCountAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> GetLockoutEnabledAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.LockoutEnabled);
        }

        public Task<DateTimeOffset> GetLockoutEndDateAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.LockoutEnd);
        }

        public Task<int> IncrementAccessFailedCountAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.AccessFailedCount++;

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task ResetAccessFailedCountAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.AccessFailedCount = 0;

            return Task.FromResult(0);
        }

        public Task SetLockoutEnabledAsync(TUser user, bool enabled)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.LockoutEnabled = enabled;

            return Task.FromResult(0);
        }

        public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset lockoutEnd)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.LockoutEnd = lockoutEnd;

            return Task.FromResult(0);
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.TwoFactorEnabled);
        }

        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.TwoFactorEnabled = enabled;

            return Task.FromResult(0);
        }

        public Task<string> GetPhoneNumberAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task SetPhoneNumberAsync(TUser user, string phoneNumber)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (phoneNumber == null)
            {
                throw new ArgumentNullException("phoneNumber");
            }

            user.PhoneNumber = phoneNumber;

            return Task.FromResult(0);
        }

        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed)
        {
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.PhoneNumberConfirmed = confirmed;

            return Task.FromResult(0);
        }

        private void ThrowIfDisposed()
        {

        }

        private async Task UpdateUserAsync(TUser user)
        {
            await this._client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_database, _collection), user);
            await _cacheProvider.SetObject(CacheHelper.CreateKey<TUser>(user.Id), user);

            var redisKeyEmail = CacheHelper.CreateKey<TUser>(user.Email);
            await _cacheProvider.SetObject(redisKeyEmail, user);
            var redisKeyEmail2 = CacheHelper.CreateKey<TUser>(user.Email);
            await _cacheProvider.SetObject(redisKeyEmail2, user);
            
            var fvt = user;
            await _cacheProvider.UpdateUser(fvt as IDatabaseModelBase);
        }

        public void Dispose()
        {
        }

        public IQueryable<TUser> Users => _client.CreateDocumentQuery<TUser>(_documentCollection);
    }
}