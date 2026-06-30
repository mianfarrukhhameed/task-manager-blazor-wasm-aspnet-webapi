using Fistix.TaskManager.WebApp.Models.Config;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Fistix.TaskManager.WebApp.Pages
{
    public partial class Authentication : ComponentBase
    {
        [Parameter] public string Action { get; set; } = null!;

        private bool _logoutStarted;

        [Inject]
        private NavigationManager Navigation { get; set; } = null!;

        [Inject]
        private Auth0Config AuthConfig { get; set; } = null!;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;

        private bool IsLogoutAction =>
            string.Equals(Action, "logout", StringComparison.OrdinalIgnoreCase);

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || _logoutStarted || !IsLogoutAction)
                return;

            _logoutStarted = true;
            await JSRuntime.InvokeVoidAsync(
                "auth0Interop.logout",
                AuthConfig.Authority.TrimEnd('/'),
                AuthConfig.ClientId,
                Navigation.BaseUri);
        }
    }
}
