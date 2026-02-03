using System.Collections.Generic;

namespace ProjectManager.Models
{
    public class DashboardViewModel
    {
        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public List<Project> RecentProjects { get; set; } = new();
        public List<TaskItem> RecentTasks { get; set; } = new();
    }
}
