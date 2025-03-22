using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectName.Domain.Entities
{
    /// <summary>
    /// Represents an agent with properties for identification, name, description, instructions, and status. Includes
    /// timestamps for creation and updates.
    /// </summary>
    public class Agent: BaseEntity
    {
        public string Instructions { get; set; } = string.Empty;

    }
}
