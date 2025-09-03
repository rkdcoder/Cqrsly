namespace Cqrsly
{
    // Command/query with return
    public interface IRequest { }

    // Command/query without return
    public interface IRequest<TResult> { }
}