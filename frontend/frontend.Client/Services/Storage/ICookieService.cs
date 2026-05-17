using System.Threading.Tasks;

namespace frontend.Client.Services.Storage
{
    public interface ICookieService
    {
        Task SetCookieAsync(string name, string value, int? minutes = null);
        Task<string?> GetCookieAsync(string name);
        Task DeleteCookieAsync(string name);
    }
}
