namespace AgentExample.Components.Pages
{
    public partial class InteractiveCodePage
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
