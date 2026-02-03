using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Models
{
    public class TaskItem
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string DocType { get; set; } = "Task";

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty; // было renamed с Name

        public string? Description { get; set; }

        [Required]
        public string ProjectId { get; set; } = string.Empty; // partition key

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? DueDate { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; } = "New";

        public string? AssignedTo { get; set; }
    }
}
