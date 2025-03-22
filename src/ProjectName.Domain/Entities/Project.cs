using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectName.Domain.Entities
{
    /// <summary>
    /// Represents a project.
    /// </summary>
    public class Project : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
