using Saun_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace Saun_App.ViewModels
{
    // Klass laiendab korrektselt ObservableObject baasklassi
    public partial class BookingViewModel : ObservableObject
    {
        private readonly SupabaseService _supabaseService;

        // 1. Käsitsi kirjutatud omadused standardse OnPropertyChanged teavitusega
        private string _houseId = "soome";
        public string HouseId
        {
            get => _houseId;
            set
            {
                if (_houseId != value)
                {
                    _houseId = value;
                    OnPropertyChanged(nameof(HouseId)); // Teavitab XAML-i automaatselt
                }
            }
        }

        private string _customerName;
        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (_customerName != value)
                {
                    _customerName = value;
                    OnPropertyChanged(nameof(CustomerName));
                }
            }
        }

        private DateTime _startDate = DateTime.Now;
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged(nameof(StartDate));
                }
            }
        }

        private DateTime _endDate = DateTime.Now.AddDays(1);
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged(nameof(EndDate));
                }
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        public BookingViewModel()
        {
            _supabaseService = new SupabaseService();
        }

        [RelayCommand]
        private async Task ConfirmBooking()
        {
            if (EndDate <= StartDate)
            {
                StatusMessage = "❌ Lahkumise kuupäev peab olema hilisem kui saabumine!";
                return;
            }

            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                StatusMessage = "❌ Palun sisesta oma nimi!";
                return;
            }

            StatusMessage = "⏳ Võetakse ühendust pilveandmebaasiga...";

            try
            {
                // Kutsume välja parandatud Supabase teenust
                bool success = await _supabaseService.BookHouseAsync(HouseId, CustomerName, StartDate, EndDate);

                if (success)
                {
                    StatusMessage = "✅ Broneering kinnitatud ja salvestatud Supabase pilvebaasi!";
                }
                else
                {
                    StatusMessage = "❌ Tõrge: Maja on nendel kuupäevadel juba broneeritud!";
                }
            }
            catch (Exception ex)
            {
                // Kui andmebaasis on viga (nt tabeli nimi vale), kuvatakse see siin
                StatusMessage = $"⚠️ Süsteemi viga: {ex.Message}";
            }
        }
    }
}