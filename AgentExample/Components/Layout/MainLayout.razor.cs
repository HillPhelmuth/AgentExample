using AgentExample.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel;

namespace AgentExample.Components.Layout;

public partial class MainLayout
{
    [Inject]
    public AgentRunnerService AgentRunner { get; set; } = default!;
    private bool sidebar1Expanded = true;
    private List<KernelPlugin> _activePlugins = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _activePlugins = await AgentRunnerService.GetActivePlugins();
            StateHasChanged();
        }
    }
    private void Reset()
    {
        AgentRunner.Reset();
    }
}