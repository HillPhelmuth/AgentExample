using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using AutoGenDotNet;
using AutoGenDotNet.Models;

namespace AgentExample.Components.Pages
{
    public partial class GroupChatPage : ComponentBase
    {
        [Inject]
        private AutoGenService AutoGenService { get; set; } = default!;
        private int _stepIndex;

        private List<BotModel> _selectedPersonas = new();
        private async void HandlePersonasSelected(List<BotModel> bots)
        {
            await AutoGenService.GenerateAgents(bots);
            _stepIndex = 0;
            StateHasChanged();
            _stepIndex = 1;
            StateHasChanged();
        }
    }
}
