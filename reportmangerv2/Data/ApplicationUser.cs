using System;
using Microsoft.AspNetCore.Identity;
using reportmangerv2.Domain;

namespace reportmangerv2.Data;

public class ApplicationUser:IdentityUser
{
    public required string FullName { get; set; }
    public bool IsActive { get; set; } = true;
   


}
