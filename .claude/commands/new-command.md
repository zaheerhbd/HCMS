Scaffold a new MediatR **Command** (write operation) in the HCMS Clean Architecture solution.

Command name: **$ARGUMENTS**

Follow these rules exactly:

## What to create

1. **`src/HCM.Application/Features/<Feature>/Commands/<CommandName>/`**
   - `<CommandName>Command.cs` — record implementing `IRequest<Result<T>>` (use `Unit` if no return value)
   - `<CommandName>CommandHandler.cs` — class implementing `IRequestHandler<>`
   - `<CommandName>CommandValidator.cs` — FluentValidation `AbstractValidator<>` for the command

2. **`src/HCM.Application/Common/Models/Result.cs`** — use existing `Result<T>` if it exists; do not duplicate.

## Conventions

- Namespace: `HCM.Application.Features.<Feature>.Commands.<CommandName>`
- Handler returns `Result<T>` or `Result<Unit>` — never throws for business errors; use `Result.Failure(message)`.
- Validator is picked up automatically by the `ValidationBehavior` MediatR pipeline — no manual wiring needed.
- No business logic in controllers; the controller just dispatches the command via `_mediator.Send(command)`.
- No raw SQL — use EF Core only via `IApplicationDbContext`.
- No PHI in log messages — log entity IDs only.

## Hard rules (from CLAUDE.md)

- Domain has no dependencies.
- Application depends only on Domain.
- Infrastructure implements Application interfaces.
- FluentValidation registered as MediatR pipeline behavior — always create a validator.

Infer the `<Feature>` folder name from the command name (e.g., `CreatePatientCommand` → `Features/Patients/Commands/CreatePatient/`).

After creating the files, show the full file tree of what was created and remind me to wire the controller endpoint if this is a new route.
