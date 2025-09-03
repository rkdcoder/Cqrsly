# Cqrsly – Minimal CQRS Dispatcher for .NET (8+)

<p align="center">
  <img src="https://raw.githubusercontent.com/rkdcoder/Cqrsly/main/src/Cqrsly/Media/icon.png" width="128" alt="Cqrsly logo" />
</p>

[![NuGet](https://img.shields.io/nuget/v/Cqrsly.svg)](https://www.nuget.org/packages/Cqrsly)
[![Build & Publish](https://github.com/rkdcoder/Cqrsly/actions/workflows/main.yml/badge.svg)](https://github.com/rkdcoder/Cqrsly/actions/workflows/main.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**Cqrsly** is a plug-and-play **CQRS dispatcher** for .NET 8+ with a MediatR-like API. It provides a minimal, high-performance setup for:

- **Commands & Queries** with return types
- **Pipeline behaviors** (logging, validation, etc.)
- **Notifications (domain events)** with sequential dispatch
- **Fluent API registration** for easy integration
- Minimal, production-ready setup: 1–2 lines in `Program.cs`

---

## Quickstart

### 1) Install

```bash
dotnet add package Cqrsly
```

---

### 2) Register in Program.cs

```csharp
using Cqrsly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCqrsly(cfg => cfg
    .AddHandlersFromAssemblyContaining<CreateAlertCommand>()
);

var app = builder.Build();

app.MapControllers();
app.Run();
```

---

### 3) Create a Command

```csharp
using Cqrsly;
using Teste.Dto;

public sealed class CreateAlertCommand : IRequest<CommandResultDto<object?>>
{
    public string Title { get; }
    public string Content { get; }

    public CreateAlertCommand(string title, string content)
    {
        Title = title;
        Content = content;
    }
}
```

---

### 4) Handle the Command

```csharp
using Cqrsly;
using Teste.Command;
using Teste.Dto;

public sealed class CreateAlertCommandHandler : IRequestHandler<CreateAlertCommand, CommandResultDto<object?>>
{
    private readonly IAlertCommandRepository _repository;

    public CreateAlertCommandHandler(IAlertCommandRepository repository)
        => _repository = repository;

    public async Task<CommandResultDto<object?>> Handle(CreateAlertCommand request, CancellationToken ct)
    {
        var id = await _repository.CreateAlertAsync(request.Title, request.Content, ct);
        var payload = new { Id = id, request.Title };
        return CommandResultDto<object?>.SuccessResult("Alert created.", payload);
    }
}
```

---

### 5) Use in a Controller

```csharp
[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly ICqrsly _cqrsly;
    public AlertsController(ICqrsly cqrsly) => _cqrsly = cqrsly;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAlertDto dto, CancellationToken ct)
    {
        var cmd = new CreateAlertCommand(dto.Title, dto.Content);

        // Duas opções de chamada:
        // 1) Estilo MediatR (inferência do tipo de retorno)
        var result = await _cqrsly.Send(cmd, ct);

        // 2) Estilo explícito (TRequest, TResponse)
        // var result = await _cqrsly.Send<CreateAlertCommand, CommandResultDto<object?>>(cmd, ct);

        return Ok(result);
    }
}
```

---

## Query Example (for tests)

```csharp
using Cqrsly;
using Teste.Dto;

public sealed class GetAlertsQuery : IRequest<CommandResultDto<IReadOnlyList<AlertDto>>>
{
    public string? TitleContains { get; }
    public GetAlertsQuery(string? titleContains) => TitleContains = titleContains;
}

public sealed class GetAlertsQueryHandler : IRequestHandler<GetAlertsQuery, CommandResultDto<IReadOnlyList<AlertDto>>>
{
    public Task<CommandResultDto<IReadOnlyList<AlertDto>>> Handle(GetAlertsQuery request, CancellationToken ct)
    {
        var list = new List<AlertDto> {
            new AlertDto { Id = 1, Title = "Test", Content = "Mocked" }
        };
        return Task.FromResult(CommandResultDto<IReadOnlyList<AlertDto>>.SuccessResult("Query executed.", list));
    }
}
```

---

## Features

- **Commands & Queries** with strong typing (`IRequest` and `IRequest<T>`)
- **Pipeline behaviors** (`IPipelineBehavior<TReq,TRes>`) for logging, validation, telemetry
- **Notifications** (`INotification`) with sequential dispatch
- **Fluent API** registration (`AddCqrsly(cfg => ...)`)
- **Lightweight & high-performance** (delegate-based pipeline, no extra deps)
- **MediatR-like ergonomics** (Send, Publish, IRequest<T>, INotification)

---

## License

**MIT**

---

## Links

- Repository: [https://github.com/rkdcoder/Cqrsly](https://github.com/rkdcoder/Cqrsly)
- NuGet: [https://www.nuget.org/packages/Cqrsly](https://www.nuget.org/packages/Cqrsly)
