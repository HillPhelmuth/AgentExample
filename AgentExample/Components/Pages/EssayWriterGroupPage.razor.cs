using Microsoft.AspNetCore.Components;

namespace AgentExample.Components.Pages
{
    public partial class EssayWriterGroupPage : ComponentBase
    {
        private int _step;
        private void Next()
        {
            _step = 0;
            StateHasChanged();
            _step = 1;
            StateHasChanged();
        }
    }
}
