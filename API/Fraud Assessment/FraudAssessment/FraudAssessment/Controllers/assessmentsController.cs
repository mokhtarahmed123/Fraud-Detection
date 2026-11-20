using FraudAssessment.Application.Features.Assessments.Commands.Model;
using FraudAssessment.Application.Features.Assessments.Queries.Model;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FraudAssessment.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AssessmentsController : ControllerBase
    {
        private readonly ISender _sender;

        public AssessmentsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Add(
       [FromBody] CreateAssessmentCommand command,
       [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
       CancellationToken cancellationToken)
        {
            var request = new CreateAssessmentRequest(
                command,
                idempotencyKey);

            var result = await _sender.Send(request, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, result);
        }
        [HttpPost("Payment")]
        public async Task<IActionResult> AddPayment(
       [FromBody] CreatePaymentAssessmentCommand command,
       [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
       CancellationToken cancellationToken)
        {
            var request = new CreatePaymentAssessmentRequest(
                command,
                idempotencyKey);

            var result = await _sender.Send(request, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpGet("{clientId}")]
        public async Task<IActionResult> GetAllByClientId(
            [FromRoute] string clientId,
            CancellationToken cancellationToken)
        {
            var request = new GetAllByClientIdQuery(clientId);
            var result = await _sender.Send(request, cancellationToken);
            return Ok(result);
        }
        [HttpGet("Id/{Id}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid Id,
            CancellationToken cancellationToken)
        {
            var request = new GetAssessmentByIdQuery(Id);
            var result = await _sender.Send(request, cancellationToken);
            return Ok(result);
        }
    }
}
