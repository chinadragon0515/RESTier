using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Microsoft.OData.Service.Sample.MongoDB.Models
{
    public class Trip
    {
        public int TripId { get; set; }

        public long? PersonId { get; set; }

        public Guid ShareId { get; set; }

        public string Name { get; set; }

        [Column("BudgetCol", Order = 1)]
        public float Budget { get; set; }

        public string Description { get; set; }

        public DateTimeOffset StartsAt { get; set; }

        public DateTimeOffset EndsAt { get; set; }
    }
}