using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Models
{
    public class TeamMember
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string DocType { get; set; } = "TeamMember";

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Surname { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Role { get; set; }
        
        public string? IdentityUserId { get; set; }

    }
}
