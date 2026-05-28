using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Saun_App.Models
{
    [Table("reservations")] // Veendu, et tabeli nimi andmebaasis on väikeste tähtedega
    public class Reservation : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("house_id")] // See rida ütleb Supabase'ile, et C# "HouseId" on baasis "house_id"
        public string HouseId { get; set; }

        [Column("customer_name")]
        public string CustomerName { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }
    }
}