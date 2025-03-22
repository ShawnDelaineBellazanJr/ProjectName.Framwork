using MediatR;
using ProjectName.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectName.Application.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class AgentService
    {
        private readonly IMediator _mediator;

        public AgentService(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// /
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="instructions"></param>
        /// <returns></returns>
        public async Task<Agent> CreateAgent(string name, string description, string instructions)
        {
            var agent = new Agent
            {
                Instructions = instructions
            };

            // In a real implementation, you would send a command through MediatR
            // await _mediator.Send(new CreateAgentCommand(agent));

            return await Task.FromResult(agent);
        }
    }
}
