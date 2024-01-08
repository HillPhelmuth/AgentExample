using AgentExample.Models;
using AgentExample.Services;
using ChatComponents;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;


namespace AgentExample.Components.Pages
{
    public partial class Home
    {
        [Inject]
        private AgentRunnerService AgentRunnerService { get; set; } = default!;
        private ChatView _chatView;
        private bool _isBusy;
        private CancellationTokenSource _cancellationTokenSource = new();
        
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                AgentRunnerService.SendMessage += HandleSendMessage;
                AgentRunnerService.ChatReset += Reset;
                await ExecuteChatSequence(AgentRunnerService.ChatStream("", _cancellationTokenSource.Token));
            }
            await base.OnAfterRenderAsync(firstRender);
        }
        
        private async void HandleChatInput(UserInputRequest request)
        {
            _isBusy = true;
            StateHasChanged();
            await Task.Delay(1);
            var input = request.ChatInput ?? "";
            _chatView!.ChatState?.AddUserMessage(input, _chatView!.ChatState.ChatMessages.Count + 1);
            var chatWithPlanner = AgentRunnerService.ChatStream(input, _cancellationTokenSource.Token);
            await ExecuteChatSequence(chatWithPlanner);
            _isBusy = false;
            StateHasChanged();

        }
        private void Cancel() => _cancellationTokenSource.Cancel();

        private async void Reset()
        {
            _chatView.ChatState?.Reset();
            StateHasChanged();
            await ExecuteChatSequence(AgentRunnerService.ChatStream("", _cancellationTokenSource.Token));
        }
        private async Task ExecuteChatSequence(IAsyncEnumerable<string> chatWithPlanner)
        {
            var hasStarted = false;
            var lastIsAssistantMessage = _chatView.ChatState?.ChatMessages.LastOrDefault()?.Role == Role.Assistant;
            await foreach (var text in chatWithPlanner)
            {
                if (lastIsAssistantMessage || hasStarted)
                {
                    _chatView!.ChatState!.UpdateAssistantMessage(text);
                }
                else
                {
                    _chatView!.ChatState!.AddAssistantMessage(text, _chatView!.ChatState.ChatMessages.Count + 1);
                    _chatView.ChatState.ChatMessages.LastOrDefault(x => x.Role == Role.Assistant)!.IsActiveStreaming = true;
                    hasStarted = true;
                }
            }

            var lastAsstMessage =
                _chatView.ChatState!.ChatMessages.LastOrDefault(x => x.Role == Role.Assistant);
            if (lastAsstMessage is not null)
                lastAsstMessage.IsActiveStreaming = false;
        }
        private void HandleSendMessage(string text)
        {
            var lastMessage = _chatView.ChatState?.ChatMessages.LastOrDefault();
            var lastIsAssistantMessage = lastMessage?.Role == Role.Assistant;
            if (lastIsAssistantMessage)
            {
                _chatView!.ChatState!.UpdateAssistantMessage(text);
            }
            else
            {
                _chatView!.ChatState!.AddAssistantMessage(text, _chatView!.ChatState.ChatMessages.Count + 1);
                _chatView.ChatState.ChatMessages.LastOrDefault(x => x.Role == Role.Assistant)!.IsActiveStreaming = true;
            }
        }
    }
}
