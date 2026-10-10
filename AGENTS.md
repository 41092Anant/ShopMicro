# Project Rules - ShopMicro

- **Target .NET 10 consistently** across projects, CI, and container images.
- **Implement only the task explicitly requested.** Do not jump to later phases.
- **Keep each service simple:** controller + small service class + DbContext.
- **Do not introduce Clean Architecture, DDD layers, CQRS frameworks, or extra class libraries** unless explicitly asked.
- **Database-per-service:** A service owns its database. Never query another service's database directly.
- **No shared EF entities between services.** Use explicit API DTOs and event contracts.
- **Asynchronous by default:** Use async I/O and pass `CancellationToken` through request and data-access paths.
- **Input validation & error handling:** Validate input and return appropriate HTTP status codes and RFC 7807 `ProblemDetails` error responses.
- **Security:** Never hardcode production secrets or commit credentials. Use local environment configuration.
- **Testing:** Add/update tests for every behavior change. Verify tests actually pass before marking done.
- **Reporting:** Before finishing, report files changed, behavior added, commands run, test results, and known gaps.
- **No unrelated refactoring:** Do not refactor unrelated code or install packages without explaining why.
- **Documentation:** Keep `README.md` and `docs/DECISIONS.md` updated when architecture or commands change.
