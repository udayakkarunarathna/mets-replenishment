using System.ComponentModel.DataAnnotations;

namespace METS.Api.Models;

public class AppUser
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public ICollection<ReplenishmentRequest> CreatedRequests { get; set; } = new List<ReplenishmentRequest>();
    public ICollection<ReplenishmentRequest> ReviewedRequests { get; set; } = new List<ReplenishmentRequest>();
}
