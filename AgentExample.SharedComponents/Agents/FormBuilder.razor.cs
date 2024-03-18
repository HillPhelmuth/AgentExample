using AgentExample.SharedComponents.ModalWindows;
using AutoGen.Core;
using AutoGenDotNet.Models;
using AutoGenDotNet;
using AutoGenDotNet.Models.AgentClasses;
using ChatComponents;
using Microsoft.AspNetCore.Components;
using Radzen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AgentExample.SharedServices.Plugins.FormPlugin;

namespace AgentExample.SharedComponents.Agents
{
    public partial class FormBuilder : ComponentBase
    {
        [Inject]
        private AutoGenService AutoGenService { get; set; } = default!;
        [Inject]
        private DialogService DialogService { get; set; } = default!;
        [Parameter]
        public GroupChatType GroupChatType { get; set; }
        private ChatView? _chatView;
        private bool _isBusy;
        private bool _requireInput;
        private IInteractiveAgent? _requestingAgent;
        private string _currentAgent = "";
        private string _summary = "";
        private CancellationTokenSource _cancellationTokenSource = new();
        private string _css => _requireInput ? "blinking-input" : ""; 
        private bool _isNext = true;
        private FakeForm _fakeForm = new();
        protected override Task OnInitializedAsync()
        {
            AutoGenService.OnMessage += HandleReadMessage;
            AutoGenService.RequestInput += HandleRequireResponse;
            AutoGenService.OnConversationComplete += HandleChatComplete;
            AutoGenService.FakeFormUpdate += UpdateForm;
            return base.OnInitializedAsync();
        }
        private async void HandleChatInput(UserInputRequest chatinput)
        {

        }
        private async Task Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            _isBusy = true;
            StateHasChanged();
            await AutoGenService.ExecuteFormBuilderAgentGroup(token);
            
        }
        private void UpdateForm(FakeForm fakeForm)
        {
            _fakeForm = fakeForm;
            InvokeAsync(StateHasChanged);
        }
        private void HandleChatComplete(string summary)
        {
            _summary = summary;
            _isBusy = false;
            StateHasChanged();
        }
        private void ShowSummary()
        {
            var options = new DialogOptions { Width = "90vw", Height = "90vh", CloseDialogOnOverlayClick = true, ShowTitle = true, Draggable = true, Resizable = true };
            DialogService.Open<ConvoSummary>("Conversation Summary", new Dictionary<string, object> { { "ConversationSummary", _summary } }, options);
        }
        private void HandleReadMessage(IMessage messageItem)
        {
            var message = messageItem.GetContent();
            var from = messageItem.From ?? "";

            _isNext = _currentAgent != from;
            _currentAgent = from;
            if (message?.StartsWith("From ", StringComparison.OrdinalIgnoreCase) == true)
                return;
            message = $"{from}:<br/>\n{message}";
            if (_isNext)
            {
                _chatView.ChatState.AddAssistantMessage(message);
                _isNext = false;
            }
            else
            {
                _chatView.ChatState.UpdateAssistantMessage(message);
            }
            StateHasChanged();
        }
        private void HandleRequireResponse(object? sender, AgentRequestEventArgs args)
        {
            _requireInput = true;
            _isBusy = false;
            _requestingAgent = args.Agent;
            StateHasChanged();
        }
        private void Cancel()
        {
            _cancellationTokenSource.Cancel();
            _isBusy = false;
            StateHasChanged();
        }
    }
}
