namespace Core.Application.CQRS;

public interface IQuery<TResponse> : IRequest<TResponse> { }
