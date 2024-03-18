using AgentExample.SharedComponents.ModalWindows;
using AutoGen.Core;
using AutoGenDotNet;
using AutoGenDotNet.Models;
using AutoGenDotNet.Models.AgentClasses;
using ChatComponents;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace AgentExample.SharedComponents.Agents
{
    public partial class GroupChat
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
        protected override Task OnInitializedAsync()
        {
            AutoGenService.OnMessage += HandleReadMessage;
            AutoGenService.RequestInput += HandleRequireResponse;
            AutoGenService.OnConversationComplete += HandleChatComplete;
            return base.OnInitializedAsync();
        }
        protected override async Task OnParametersSetAsync()
        {
            if (GroupChatType == GroupChatType.FormBuilder)
            {
                _isBusy = true;
                StateHasChanged();
                // ReSharper disable once MethodSupportsCancellation
                await Task.Delay(1);
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;
                await AutoGenService.ExecuteFormBuilderAgentGroup(token);
            }
            await base.OnParametersSetAsync();
        }
        private async void HandleChatInput(UserInputRequest chatinput)
        {
            var input = chatinput.ChatInput;
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            _chatView.ChatState.AddUserMessage(input);
            _isBusy = true;
            StateHasChanged();
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(1);

            if (_requireInput)
            {
                _requireInput = false;
                _requestingAgent?.ProvideInput(input);
            }
            else switch (GroupChatType)
            {
                case GroupChatType.DotnetInteractive:
                    await AutoGenService.RunDotnetInteractiveWorkflowGroupChat(input, token);
                    break;
                case GroupChatType.EssayWriter:
                    await AutoGenService.StartEssayResearchWriterAgentGroup(input, token);
                    break;
                case GroupChatType.Persona:
                    await AutoGenService.StartGroupChat(input, token);
                    break;
                case GroupChatType.FormBuilder:
                    await AutoGenService.ExecuteFormBuilderAgentGroup(token);
                    break;
                default:
                    await AutoGenService.StartGroupChat(input, token);
                    break;
            }

        }
        private void HandleChatComplete(string summary)
        {
            _summary = summary;
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
