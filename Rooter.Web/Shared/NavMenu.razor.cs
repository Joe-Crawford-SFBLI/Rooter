using Microsoft.AspNetCore.Components;

namespace Rooter.Web.Shared;

public partial class NavMenu : ComponentBase
{
    private bool collapseNavMenu = true;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    private void _ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }

    private void _CollapseNavMenu()
    {
        collapseNavMenu = true;
    }
}
