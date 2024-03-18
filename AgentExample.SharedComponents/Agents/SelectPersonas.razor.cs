using AutoGenDotNet.Models;
using Microsoft.AspNetCore.Components;

namespace AgentExample.SharedComponents.Agents
{
    public partial class SelectPersonas : ComponentBase
    {
        private List<BotModel> _personas = [];
        [Parameter]
        public EventCallback<List<BotModel>> PersonasSelected { get; set; }
        //[Parameter]
        //public EventCallback RunDotNetInteractive { get; set; }
        private List<BotModel> _selectedPersonas = [];
        private void AddPersona(BotModel persona)
        {
            if (_selectedPersonas.Contains(persona)) return;
            _selectedPersonas.Add(persona);
            StateHasChanged();
        }
        private void RemovePersona(BotModel persona)
        {
            if (!_selectedPersonas.Contains(persona)) return;
            _selectedPersonas.Remove(persona);
            StateHasChanged();
        }
        //private void RunDotnet()
        //{
        //    RunDotNetInteractive.InvokeAsync();
        //}
        protected override Task OnInitializedAsync()
        {
            _personas = BotModel.GetAllPremadeBots().ToList();
            return base.OnInitializedAsync();
        }
        private Task CompleteSelection()
        {
            return PersonasSelected.InvokeAsync(_selectedPersonas);
        }
    }
}
