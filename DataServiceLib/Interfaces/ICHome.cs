using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICHome
    {
        Task<CResponseMessage> MovieLastestItem();
    }
}