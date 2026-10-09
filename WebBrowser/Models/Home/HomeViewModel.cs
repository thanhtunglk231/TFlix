using WebBrowser.Models.Movie;
using CoreLib.Dtos.Payment;

namespace WebBrowser.Models.Home
{
    public class HomeViewModel
    {
        public List<MovieItem> Movies { get; set; } = new();
        public List<SubscriptionPlanItemDto> SubscriptionPlans { get; set; } = new();
        public SubscriptionStatusDto SubscriptionStatus { get; set; } = new();
    }
}
