using AutoGen.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AgentExample.SharedServices.Models
{
    public static class ObjectHelpers
    {
        public static string CheckPropertiesAndValues<T>(T obj)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var sb = new StringBuilder();
            var isComplete = true;
            foreach (var property in properties)
            {
                var name = property.Name;
                var value = property.GetValue(obj, null); // null for index parameter for non-indexed properties

                // Handling the case where the value is null to prevent printing "Value: "
                var isNull = value is null || string.IsNullOrEmpty(value?.ToString());
                if (isNull)
                    isComplete = false;
                var valueString = isNull ? "is missing" : value!.ToString();
                var format = $"{name} -- {valueString}";
                Console.WriteLine(format);
                sb.AppendLine(format);
                sb.AppendLine();
            }
            return isComplete ? sb.ToString() + "\n Application information is saved to database." : sb.ToString();
        }
    }
}
