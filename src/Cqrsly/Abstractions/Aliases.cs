namespace Cqrsly
{
    // Aliases semânticos — ICommand/IQuery/IEvent
    public interface ICommand : IRequest { }
    public interface ICommand<out TResponse> : IRequest<TResponse> { }

    public interface IQuery<out TResponse> : IRequest<TResponse> { }

    public interface IEvent : INotification { }
}