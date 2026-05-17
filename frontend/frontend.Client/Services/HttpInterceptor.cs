using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace frontend.Client.Services
{
    public class HttpInterceptor : DelegatingHandler
    {
        private readonly ITokenStorageService _tokenStorage;
        private readonly NavigationManager _navigationManager;

        public HttpInterceptor(ITokenStorageService tokenStorage, NavigationManager navigationManager)
        {
            _tokenStorage = tokenStorage;
            _navigationManager = navigationManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenStorage.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token is invalid or expired
                await _tokenStorage.RemoveTokenAsync();
                _navigationManager.NavigateTo("/login");
            }
            
            return response;
        }
    }
}
