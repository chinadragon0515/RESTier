using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using MongoDB.Bson;

namespace Microsoft.OData.Service.Sample.MongoDB.Models
{
    public class Person
    {
        public virtual Person BestFriend { get; set; }

        public virtual ICollection<Person> Friends { get; set; }

        public virtual ICollection<Trip> Trips { get; set; }

        public ObjectId _id { get; set; }

        public long PersonId { get; set; }

        public string UserName { get; set; }

        [Required]
        public string FirstName { get; set; }

        [MaxLength(26), MinLength(1)]
        public string LastName { get; set; }

        public long? Age { get; set; }
    }
}