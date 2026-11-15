using WebBrowser.Models.Movie;
using WebBrowser.Models.Genres;
using CoreLib.Dtos.Payment;

namespace WebBrowser.Models.Home
{
    public class HomeViewModel
    {
        public List<MovieItem> Movies { get; set; } = new();
        public List<GenreItem> Genres { get; set; } = new();
        public string? GenresErrorMessage { get; set; }
        public List<SubscriptionPlanItemDto> SubscriptionPlans { get; set; } = new();
        public SubscriptionStatusDto SubscriptionStatus { get; set; } = new();
    }
}
