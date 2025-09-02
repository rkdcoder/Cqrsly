namespace Cqrsly
{
    // request sem retorno
    public interface IRequest { }

    // request com retorno
    public interface IRequest<out TResponse> { }
}