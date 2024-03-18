using AgentExample.SharedServices.Services;
using AutoGenDotNet;
using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel;

namespace AgentExample.Components.Layout;

public partial class MainLayout
{
    [Inject]
    public SkAgentRunnerService AgentRunner { get; set; } = default!;
    [Inject]
    public AutoGenService AutoGenService { get; set; } = default!;
    [CascadingParameter(Name = "PageTitle")]
    public string PageTitle { get; set; } = default!;
    private bool sidebar1Expanded = true;
    private List<KernelPlugin> _activePlugins = [];
    private void ShowDisplay(string path)
    {

    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _activePlugins = await SkAgentRunnerService.GetActivePlugins();
            StateHasChanged();
        }
    }
    private void Reset()
    {
        AgentRunner.Reset();
    }
}