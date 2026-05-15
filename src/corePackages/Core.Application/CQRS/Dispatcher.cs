using Microsoft.Extensions.DependencyInjection;

namespace Core.Application.CQRS;

public sealed class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var wrapper = (IRequestHandlerWrapper<TResponse>)Activator.CreateInstance(wrapperType)!;
        return wrapper.Handle(request, _serviceProvider, cancellationToken);
    }
}

internal interface IRequestHandlerWrapper<TResponse>
{
    Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider sp, CancellationToken ct);
}

internal sealed class RequestHandlerWrapper<TRequest, TResponse> : IRequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider sp, CancellationToken ct)
    {
        var handler = sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = sp.GetServices<IPipelineBehavior<TRequest, TResponse>>();

        RequestHandlerDelegate<TResponse> invoke = () => handler.Handle((TRequest)request, ct);

        return behaviors
            .Reverse()
            .Aggregate(invoke, (next, behavior) => () => behavior.Handle((TRequest)request, next, ct))();
    }
}
