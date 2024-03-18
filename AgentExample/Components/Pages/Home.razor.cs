using ChatComponents;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;
using System.Text;
using AgentExample.SharedServices.Models;
using AgentExample.SharedServices.Plugins.TaxExpertPlugin;
using AgentExample.SharedServices.Services;
using AutoGenDotNet;
using System.Text.Json;


namespace AgentExample.Components.Pages;

public partial class Home
{
    [Inject]
    private SkAgentRunnerService AgentRunnerService { get; set; } = default!;
    [Inject]
    private AutoGenService AutoGenService { get; set; } = default!;
    private ChatView _chatView;
    private bool _isBusy;
    private CancellationTokenSource _cancellationTokenSource = new();
    private List<string> _logs = [];
    private int _logCount = 0;
    private string _outputObjectJson = "";

    
}