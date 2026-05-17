using System.Collections.Generic;
using System.Threading.Tasks;

namespace frontend.Client.Services
{
    public class LookupService
    {
        // Mocking the backend API for now since /api/lookups doesn't exist yet
        // In a real scenario, this would use IBackendClient

        public Task<List<string>> GetRolesAsync()
        {
            return Task.FromResult(new List<string>
            {
                "Developer",
                "UI/UX Designer",
                "Quality Control",
                "Project Manager"
            });
        }

        public Task<List<string>> GetDivisionsAsync()
        {
            return Task.FromResult(new List<string>
            {
                "Tech",
                "Business",
                "Marketing",
                "HR"
            });
        }
    }
}
