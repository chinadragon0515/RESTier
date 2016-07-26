using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.OData.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Service.Sample.MongoDB.Models;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Providers.MongoDB;
using MongoDB.Driver;

namespace Microsoft.OData.Service.Sample.MongoDB.Api
{
    public class TrippinMongoDbApi : MongoDbApi
    {
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            // TODO, hard code a IMongoDatabase instance into DI now....
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("trippin");
            services.AddSingleton<IMongoDatabase>(database);
            prepareSomeData(database);

            return base.ConfigureApi(services)
                .AddService<IModelBuilder, TrippinModelExtender>();
        }

        private void prepareSomeData(IMongoDatabase database)
        {
            // database.DropCollection("People");

            var collection = database.GetCollection<Person>("People");

            // Remove all entity first
            var filter = Builders<Person>.Filter.Empty;
            collection.DeleteMany(filter);

            // Add some data for testing
            var person0 = new Person
            {
                PersonId = 0,
                FirstName = "Russell",
                LastName = "Whyte",
                UserName = "russellwhyte",
            };

            var person1 = new Person
            {
                PersonId = 1,
                FirstName = "Scott",
                LastName = "Ketchum",
                UserName = "scottketchum",
                Friends = new Collection<Person> { person0 }
            };

            var person2 = new Person
            {
                PersonId = 2,
                FirstName = "Ronald",
                LastName = "Mundy",
                UserName = "ronaldmundy",
                Friends = new Collection<Person> { person0, person1 }
            };

            var person3 = new Person
            {
                PersonId = 3,
                FirstName = "Javier",
                UserName = "javieralfred",
            };

            var person4 = new Person
            {
                PersonId = 4,
                FirstName = "Willie",
                LastName = "Ashmore",
                UserName = "willieashmore",
                BestFriend = person3,
                Friends = new Collection<Person>()
            };

            var person5 = new Person
            {
                PersonId = 5,
                FirstName = "Vincent",
                LastName = "Calabrese",
                UserName = "vincentcalabrese",
                BestFriend = person4,
                Friends = new Collection<Person>()
            };

            var person6 = new Person
            {
                PersonId = 6,
                FirstName = "Clyde",
                LastName = "Guess",
                UserName = "clydeguess"
            };

            var collectionPerson =  new Collection<Person> { person1, person2, person3, person4, person5, person6 };
            collection.InsertOne(person0);
            collection.InsertMany(collectionPerson);
        }

        private class TrippinModelExtender : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var builder = new ODataConventionModelBuilder();
                builder.EntitySet<Person>("People");
                builder.EntitySet<Trip>("Trips");
                return Task.FromResult(builder.GetEdmModel());
            }
        }
    }
}