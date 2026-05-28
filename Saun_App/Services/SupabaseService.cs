using Saun_App.Models;
using Supabase;


namespace Saun_App.Services
{
    public class SupabaseService
    {
        private readonly Client _supabaseClient;

        public SupabaseService()
        {
            // URL ja API võti on paigas
            string url = "https://agsocpclacqwrbxbvmpa.supabase.co";
            string key = "sb_publishable_HhxJ2THI7QsgSrGix9I4TA_frsEqPpv";

            _supabaseClient = new Client(url, key);
        }

        // REAALAJAS KONTROLL JA BRONEERIMINE
        public async Task<bool> BookHouseAsync(long houseId, string customerName, DateTime start, DateTime end)
        {
            try
            {
                // PARANDUS: Lisasime houseId taha .ToString(), et vältida tüübiviga C# teegis
                var response = await _supabaseClient
                    .From<Reservation>()
                    .Filter("house_id", Postgrest.Constants.Operator.Equals, houseId.ToString())
                    .Get();

                // Kui andmebaasi tabel on täiesti tühi, saame kohe broneerida
                if (response?.Models == null || !response.Models.Any())
                {
                    return await InsertReservation(houseId, customerName, start, end);
                }

                // 2. Võtame sisendist puhtalt kuupäeva
                DateTime startOnly = start.Date;
                DateTime endOnly = end.Date;

                // 3. Kontrollime kuupäevade kattuvust
                bool isOverlapping = response.Models.Any(r =>
                {
                    DateTime dbStart = r.StartDate.Date;
                    DateTime dbEnd = r.EndDate.Date;

                    return startOnly < dbEnd && endOnly > dbStart;
                });

                if (isOverlapping)
                {
                    return false; // Kuupäevad kattuvad!
                }

                // 4. Kui kuupäevad on vabad, salvestame
                return await InsertReservation(houseId, customerName, start, end);
            }
            catch (Exception ex)
            {
                throw new Exception($"Supabase andmebaasi viga: {ex.Message}");
            }
        }

        // Muuda ka siin string houseId -> long houseId
        private async Task<bool> InsertReservation(long houseId, string customerName, DateTime start, DateTime end)
        {
            var newReservation = new Reservation
            {
                HouseId = houseId, // See on nüüd korrektselt long number
                CustomerName = customerName,
                StartDate = start.Date,
                EndDate = end.Date
            };

            await _supabaseClient.From<Reservation>().Insert(newReservation);
            return true;
        }

       

        //------------------------2. osa - MAJADE NÄITAMINE------------------------
        public async Task<List<House>> GetHousesAsync()
        {
            try
            {
                var response = await _supabaseClient.From<House>().Get();
                return response.Models ?? new List<House>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Majade laadimise viga: {ex.Message}");
            }
        }

        // 1. Laeb andmebaasist konkreetse kliendi broneeringud
        public async Task<List<Reservation>> GetClientReservationsAsync(string customerName)
        {
            try
            {
                var response = await _supabaseClient
                    .From<Reservation>()
                    .Filter("customer_name", Postgrest.Constants.Operator.Equals, customerName)
                    .Get();

                return response.Models ?? new List<Reservation>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Broneeringute laadimise viga: {ex.Message}");
            }
        }

        // 2. Kustutab broneeringu andmebaasist ID järgi (DELETE)
        public async Task<bool> DeleteReservationAsync(long reservationId)
        {
            try
            {
                await _supabaseClient
                    .From<Reservation>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, reservationId.ToString())
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Tühistamise viga: {ex.Message}");
            }
        }


    }
}