using System;
using System.ComponentModel.DataAnnotations;

namespace reportmangerv2.ViewModels;

public class RegisterViewModel
{
    public required string Name { get; set; }
    [EmailAddress]

    public required string Email { get; set; }
    [DataType(DataType.Password)]
    public required string Password { get; set; }
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password",
    ErrorMessage = "The password and confirmation password do not match.")]
    public required string ConfirmPassword { get; set; }

}
