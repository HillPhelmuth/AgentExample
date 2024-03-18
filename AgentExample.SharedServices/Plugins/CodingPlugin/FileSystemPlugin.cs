using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentExample.SharedServices.Plugins.CodingPlugin
{
    public class FileSystemPlugin
    {
        [KernelFunction, Description("Save the text of a file.")]
        public async Task<string> SaveFile([Description("Location to save the file to including the file name")]string fullFilePath,[Description("Text you're saving to file")] string fileContent)
        {

            await File.WriteAllTextAsync(fullFilePath, fileContent);
            return "File saved";
        }
    }
}
