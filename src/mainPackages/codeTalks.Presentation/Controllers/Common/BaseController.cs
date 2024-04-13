using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.Presentation.Controllers.Common;


[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    protected IMediator? mediator => _mediator ??= HttpContext.RequestServices.GetService<IMediator>();
    private IMediator? _mediator;

}
