using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgentExample.Components.Pages
{
    public partial class FormBuilderAgentPage : ComponentBase
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
