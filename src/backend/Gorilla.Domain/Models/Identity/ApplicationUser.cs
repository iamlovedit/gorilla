namespace Gorilla.Domain.Models.Identity;

public class ApplicationUser : EntityBase<long>
{
    public required string Email { get; set; }

    public DateTimeOffset LastLoginDate { get; set; }
    
}