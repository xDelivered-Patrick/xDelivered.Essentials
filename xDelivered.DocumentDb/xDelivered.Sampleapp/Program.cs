using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xDelivered.DocumentDb.Models;
using xDelivered.DocumentDb.Services;
using xDelivered.Sampleapp.Helpers;
using xDelivered.Sampleapp.Models;
using xDelivered.Sampleapp.Services;

namespace xDelivered.Sampleapp
{
    class Program
    {
        private static User _user3;
        private static User _user2;
        private static User _user;

        private static SportsDataContext _dataContext = new SportsDataContext(new ConfigBase());
        private static Sport _tennis;
        private static Sport _karete;

        static void Main(string[] args)
        {
            Run();

            Console.ReadLine();
        }

        private static async Task Run()
        {
            await SetupCosmos();

            await RegisterUsers();
            await RegisterSports();
            await AddTournaments();
            await AddUsersToTournaments();

            QueryData();
        }

        private static async Task SetupCosmos()
        {
            //make sure collections are created (one-time)
            await _dataContext.Init();
        }

        private static async Task RegisterSports()
        {
            _karete = new Sport()
            {
                Name = "Karate"
            };
            _tennis = new Sport()
            {
                Name = "Tennis"
            };

            await _dataContext.UpsertDocumentAndCache(_karete);
            await _dataContext.UpsertDocumentAndCache(_tennis);
        }

        private static async Task AddUsersToTournaments()
        {
            _karete.Users.Add(_user);
            _karete.Users.Add(_user2);
            _tennis.Users.Add(_user3);

            await _dataContext.UpsertDocumentAndCache(_karete);
            await _dataContext.UpsertDocumentAndCache(_tennis);
        }
        
        private static async Task AddTournaments()
        {
            var karateTournament = new Tournament()
            {
                Name = _karete.Name,
                Sport = new ObjectLink<Sport>(_karete),
                Users = new List<ObjectLink<User>>()
                {
                    new ObjectLink<User>(_user),
                    new ObjectLink<User>(_user2)
                },
                Starts = DateTime.UtcNow.AddDays(5)
            };

            var tennisTournament = new Tournament()
            {
                Name = _tennis.Name,
                Sport = new ObjectLink<Sport>(_tennis),
                Users = new List<ObjectLink<User>>()
                {
                    new ObjectLink<User>(_user3)
                },
                Starts = DateTime.UtcNow.AddDays(14)
            };

            await _dataContext.UpsertDocumentAndCache(karateTournament);
            await _dataContext.UpsertDocumentAndCache(tennisTournament);
        }

        private static async Task RegisterUsers()
        {
            _user = new User {FirstName = "Billy", LastName = "Bob", Age = 35};
            _user2 = new User { FirstName = "Michael", LastName = "Keaton", Age = 24 };
            _user3 = new User { FirstName = "Andy", LastName = "Murray", Age = 23 };

            await _dataContext.UpsertDocumentAndCache(_user);
            await _dataContext.UpsertDocumentAndCache(_user2);
            await _dataContext.UpsertDocumentAndCache(_user3);
        }

        private static void QueryData()
        {
            //pull tournaments that are this week (pulls from cosmos)
            List<Tournament> tournaments = _dataContext.ThisWeeksTournaments();

            //who is playing?
            foreach (var tournament in tournaments)
            {
                foreach (var userLink in tournament.Users)
                {
                    //will resolve from redis
                    var user = userLink.Resolve();

                    Console.WriteLine($"{user.FullName} is playing {tournament.Name} on {tournament.Starts.ToString("D")}");
                }
            }

        }

        private async void Sample()
        {
            //use DI for this in production
            var db = new XDbProvider(
                redisConnectionString: "redisConnectionString",
                cosmosCosmosDb: new CosmosProvider("cosmosEndpoint", "cosmosKey", "cosmosCosmosDb", "mainCollection"));

            //insert into cosmos and redis
            await db.UpsertDocumentAndCache(
                new User
                {
                    FirstName = "Billy",
                    LastName = "Bob",
                    Age = 35
                });

            //pulls from database (tries Redis first, falls back to CosmosProvider)
            var user = await db.GetObjectAsync<User>("userId");

            //Create tournament and upsert
            var completedTournament = new Tournament()
            {
                Name = "tennis tournament"
            };
            await _dataContext.UpsertDocumentAndCache(completedTournament);

            _dataContext.SetObjectOnlyCache(user, TimeSpan.FromHours(3));

            //add link between user and tournament
            user.CompletedTournaments.Add(new ObjectLink<Tournament>(completedTournament));

            //now pull tournament from user object via memory/redis/cosmosDb (order of query)
            Tournament tourney = user.CompletedTournaments.First().Resolve();
        }
    }
}
