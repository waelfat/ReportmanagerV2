using reportmangerv2.Enums;

namespace reportmangerv2.ViewModels;

public class ParameterViewModel
{
    public required string Name { get; set; }
    public required string Value { get; set; }
    public string? Description { get; set; }
    public string? DisplayName { get; set; } 
    public bool IsRequired { get; set; }=false;
      public ViewControl ViewControl { get; set; }=ViewControl.TextBox;
}

