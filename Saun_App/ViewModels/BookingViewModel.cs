using Saun_App.Services;
using Saun_App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Saun_App.ViewModels
{
    public partial class BookingViewModel : ObservableObject
    {
        private readonly SupabaseService _supabaseService;

        public ObservableCollection<House> Houses { get; set; } = new ObservableCollection<House>();

        // UUS: Nimekiri kliendi aktiivsetest broneeringutest
        public ObservableCollection<Reservation> MyReservations { get; set; } = new ObservableCollection<Reservation>();

        private House _selectedHouse;
        public House SelectedHouse
        {
            get => _selectedHouse;
            set { if (_selectedHouse != value) { _selectedHouse = value; OnPropertyChanged(nameof(SelectedHouse)); } }
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

                    // Salvestame nime kohalikku mällu (võtmega "saved_customer_name")
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        Preferences.Default.Set("saved_customer_name", value);
                    }
                    else
                    {
                        Preferences.Default.Remove("saved_customer_name");
                    }

                    // Uuendame broneeringute nimekirja
                    _ = RefreshMyReservationsAsync();
                }
            }
        }

        private DateTime _startDate = DateTime.Now;
        public DateTime StartDate
        {
            get => _startDate;
            set { if (_startDate != value) { _startDate = value; OnPropertyChanged(nameof(StartDate)); } }
        }

        private DateTime _endDate = DateTime.Now.AddDays(1);
        public DateTime EndDate
        {
            get => _endDate;
            set { if (_endDate != value) { _endDate = value; OnPropertyChanged(nameof(EndDate)); } }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); } }
        }

        public BookingViewModel()
        {
            _supabaseService = new SupabaseService();
            _ = LoadHousesAsync();
            // -------- UUS: Laeme salvestatud kliendi nime, kui see on olemas
            // Loeme salvestatud nime mälust välja. Kui seal midagi pole, jääb tühjaks ("")
            string savedName = Preferences.Default.Get("saved_customer_name", string.Empty);

            if (!string.IsNullOrEmpty(savedName))
            {
                // See kutsub automaatselt esile ka RefreshMyReservationsAsync()
                CustomerName = savedName;
            }
        }

        private async Task LoadHousesAsync()
        {
            try
            {
                var dbHouses = await _supabaseService.GetHousesAsync();
                Houses.Clear();
                foreach (var house in dbHouses) { Houses.Add(house); }
            }
            catch (Exception ex) { StatusMessage = $"⚠️ Viga majade laadimisel: {ex.Message}"; }
        }

        // UUS: Meetod broneeringute nimekirja värskendamiseks
        private async Task RefreshMyReservationsAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                MyReservations.Clear();
                return;
            }

            try
            {
                var resList = await _supabaseService.GetClientReservationsAsync(CustomerName);
                MyReservations.Clear();
                foreach (var res in resList)
                {
                    MyReservations.Add(res);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"⚠️ Ei saanud broneeringuid laadida: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ConfirmBooking()
        {
            if (SelectedHouse == null || EndDate <= StartDate || string.IsNullOrWhiteSpace(CustomerName))
            {
                StatusMessage = "❌ Kontrolli andmeid! Maja, nimi või kuupäevad on valed.";
                return;
            }

            StatusMessage = "⏳ Broneeritakse...";

            try
            {
                bool success = await _supabaseService.BookHouseAsync(SelectedHouse.Id, CustomerName, StartDate, EndDate);

                if (success)
                {
                    StatusMessage = $"✅ Broneering kinnitatud! ({SelectedHouse.Name})";
                    // UUS: Uuendame kohe nimekirja ekraanil
                    await RefreshMyReservationsAsync();
                }
                else
                {
                    StatusMessage = "❌ Tõrge: See maja on nendel kuupäevadel juba võetud!";
                }
            }
            catch (Exception ex) { StatusMessage = $"⚠️ Süsteemi viga: {ex.Message}"; }
        }

        // UUS KÄSK: Broneeringu tühistamine (kustutamine)
        [RelayCommand]
        private async Task CancelReservation(Reservation reservation)
        {
            if (reservation == null) return;

            StatusMessage = "⏳ Tühistatakse broneeringut...";

            try
            {
                // Kutsume esile DELETE päringu baasi vastu (kasutame tabeli 'id' veergu, mis on piltidelt näha, et on int8/long)
                bool success = await _supabaseService.DeleteReservationAsync(reservation.Id);

                if (success)
                {
                    StatusMessage = "🗑️ Broneering edukalt tühistatud!";
                    // Värskendame ekraani
                    await RefreshMyReservationsAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"⚠️ Tühistamine ebaõnnestus: {ex.Message}";
            }
        }
    }
}