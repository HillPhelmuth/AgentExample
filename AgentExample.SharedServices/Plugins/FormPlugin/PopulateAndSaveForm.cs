using AgentExample.SharedServices.Models;
using AutoGen.Core;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;

namespace AgentExample.SharedServices.Plugins.FormPlugin;

public class PopulateAndSaveForm
{
    private FakeForm _fakeForm = new();
    public event Action<FakeForm>? FormChanged;
   
    [KernelFunction, Description("Update a form with user's first and last name")]
    [return: Description("Current status of the form")]
    public string SaveName([Description("Users first name")]string firstName,[Description("Users last name")] string lastName)
    {
        _fakeForm.FirstName = firstName;
        _fakeForm.LastName = lastName;
        FormChanged?.Invoke(_fakeForm);
        return _fakeForm.Validate();
    }
    [KernelFunction, Description("Update a form with user's address")]
    [return: Description("Current status of the form")]
    public string SaveAddress([Description("User's street number and name")] string streetAddress,[Description("User's city")] string city, [Description("User's State Abbreviation")] string stateAbbr, [Description("User's zip code")] string zip)
    {
        _fakeForm.StreetAddress = streetAddress;
        _fakeForm.City = city;
        _fakeForm.State = stateAbbr;
        _fakeForm.Zip = zip;
        FormChanged?.Invoke(_fakeForm);
        return _fakeForm.Validate();
    }
    [KernelFunction, Description("Update a form with user's phone number")]
    [return: Description("Current status of the form")]
    public string SavePhoneNumber([Description("user's phone number")]string phone)
    {
        _fakeForm.Phone = phone;
        FormChanged?.Invoke(_fakeForm);
        return _fakeForm.Validate();
    }
    [KernelFunction, Description("Update a form with user's email address")]
    [return: Description("Current status of the form")]
    public string SaveEmail([Description("User's email address")]string email)
    {
        _fakeForm.Email = email;
        FormChanged?.Invoke(_fakeForm);
        return _fakeForm.Validate();
    }
    [KernelFunction, Description("Update a form with user's preference for receiving updates")]
    [return: Description("Current status of the form")]
    public string SaveGetUpdates([Description("User's preference to receive updates")] bool getUpdates)
    {
        _fakeForm.GetUpdates = getUpdates;
        FormChanged?.Invoke(_fakeForm);
        return _fakeForm.Validate();
    }
    [KernelFunction, Description("Get the current state of the form")]
    [return: Description("Current status of the form")]
    public string GetCurrentStatus()
    {
        return _fakeForm.Validate();
    }
}
public class FakeForm
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public bool? GetUpdates { get; set; }

    public string Validate()
    {
        var checkPropertiesAndValues = ObjectHelpers.CheckPropertiesAndValues(this);
        if (checkPropertiesAndValues.Contains("Application information is saved to database."))
            return checkPropertiesAndValues + $"\n{GroupChatExtension.TERMINATE}";
        return checkPropertiesAndValues;
        //var missingInfoSb = new StringBuilder();
        //if (string.IsNullOrEmpty(FirstName)) 
        //    missingInfoSb.AppendLine("First Name is missing.");
        //if (string.IsNullOrEmpty(LastName)) 
        //    missingInfoSb.AppendLine("Last Name is missing.");
        //if (string.IsNullOrEmpty(Phone)) 
        //    missingInfoSb.AppendLine("Phone is missing.");
        //if (string.IsNullOrEmpty(Email)) 
        //    missingInfoSb.AppendLine("Email is missing.");
        //if (string.IsNullOrEmpty(StreetAddress)) 
        //    missingInfoSb.AppendLine("StreetAddress is missing.");
        //if (string.IsNullOrEmpty(City))
        //    missingInfoSb.AppendLine("City is missing.");
        //if (string.IsNullOrEmpty(State))
        //    missingInfoSb.AppendLine("State is missing.");
        //if (string.IsNullOrEmpty(Zip))
        //    missingInfoSb.AppendLine("Zip code is missing.");
        //if (GetUpdates is null)
        //    missingInfoSb.AppendLine("GetUpdates is missing.");
        //return missingInfoSb.Length > 0 ? missingInfoSb.ToString() : "Application information is saved to database.";
    }
}