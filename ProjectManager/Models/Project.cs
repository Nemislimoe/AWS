using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Models
{
    public class Project
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string DocType { get; set; } = "Project";

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public List<string>? TeamMemberIds { get; set; }
    }
}
