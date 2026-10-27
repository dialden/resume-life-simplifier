# Architecture and testing guidelines

## Architecture

- Put each top-level class, interface, record, enum, or struct in its own source
  file, named after that type.
- Use hexagonal architecture (ports and adapters). Core is at the center;
  Abstractions defines driving (inbound) ports, and Core declares driven
  (outbound) ports for capabilities it needs. API and UI are driving adapters;
  Infrastructure provides driven adapters. Dependencies point inward toward the
  domain and its contracts, never from the domain toward adapters.
- Preserve the separation between domain rules, domain-facing contracts,
  infrastructure implementations, and delivery adapters.
- Each library represents a bounded context. Use the natural language of that
  context for types, methods, and other names rather than imposing one vocabulary
  across all layers.
- [Core](../src/system/Core/DialDen.RLS/) owns domain business rules. It must not
  depend on infrastructure, UI, HTTP, persistence frameworks, or other delivery
  concepts.
- Declare interfaces for capabilities the domain needs in Core, using domain
  language. For example, a persistence requirement might be expressed as
  `IContractsArchive`, not as an Entity Framework repository.
- [Infrastructure](../src/system/Infrastructure/DialDen.RLS.Infrastructure/)
  implements those Core interfaces using the necessary technologies. For example,
  `EntityFrameworkContractsArchive` could implement `IContractsArchive`. These
  names are illustrative, not existing types or a requirement to use Entity
  Framework.
- [Abstractions](../src/system/Core/DialDen.RLS.Abstractions/) contains the records
  and interfaces through which callers drive the Core domain. Distinguish these
  driving contracts from the interfaces Core declares for capabilities it needs.
- [API](../src/system/API/DialDen.RLS.API/) and
  [UI](../src/system/UI/DialDen.RLS.UI/) are thin adapters of the contracts in
  Abstractions. Translate transport or presentation inputs into domain contracts,
  and domain outputs into transport or presentation responses. Do not implement
  business rules in these adapters.
- Use adapter-appropriate language in API and UI, such as HTTP, HTML, and
  components. Do not leak that vocabulary into Core.
- Keep executable hosting and composition concerns in the corresponding
  `.Root` projects rather than in domain code.
- Register service contracts through layer-owned `IServiceCollection`
  extension methods: Core registers its inbound contract implementations, and
  Infrastructure registers implementations of Core-declared driven ports.
  `.Root` projects compose these extensions instead of registering layer
  implementations directly. Keep Infrastructure mappings in Infrastructure,
  even when an adapter is not implemented yet; do not substitute test or
  in-memory implementations in production registration.
- Some layers are still scaffolding. Follow these intended boundaries when
  implementing features; missing wiring is not permission to bypass them.

## Test-first development

- Start each behavior change with an acceptance test that describes the customer
  experience. Run it and establish that it fails for the expected missing behavior
  before implementing the change. Then implement the behavior and rerun the test.
- Acceptance tests are coupled to observable system behavior, not implementation
  details. Refactoring internals without changing behavior should not require
  rewriting acceptance scenarios.
- Express scenarios in natural domain language through the existing
  [DSL](../src/tests/DialDen.RLS.Tests/Core/Dsl/). Keep HTTP calls, browser
  interactions, selectors, and other mechanics in the appropriate
  [drivers](../src/tests/DialDen.RLS.Tests/Core/Drivers/) and
  [clients](../src/tests/DialDen.RLS.Tests/Core/Clients/), not in scenarios.
- The system has two customer interaction channels: API and UI. Use the existing
  channel-aware test infrastructure to exercise applicable behavior through both
  channels, keeping scenarios independent of channel mechanics.
- Stub external systems at their integration boundaries to cover success,
  failure, and edge-case behaviors deterministically. Do not stub the system's
  own domain behavior in acceptance tests.
- Reuse the existing
  [test environments](../src/tests/DialDen.RLS.Tests/Fixtures/), including their
  real/stubbed external-system distinction, rather than introducing a parallel
  test harness.
- Unit tests are not part of the current testing approach; they may be added
  later. Do not introduce them unless requested, and do not substitute them for
  acceptance tests.
- Verify the changed behavior with focused acceptance tests, then run broader
  relevant coverage as needed. Report validation failures or unavailable
  prerequisites explicitly; do not claim unexecuted tests passed.

## Acceptance-test DSL

- Treat the test harness as ports and adapters too: scenarios speak the language
  of customer behavior, channel-neutral drivers define interactions with the
  system, and API/UI driver implementations adapt those interactions to clients.
  Clients own HTTP and browser mechanics.
- Preserve the existing `Scenario(channel).Assume().RLS().ShouldBeRunning()`
  smoke-test style. As features are added, extend the DSL with fluent
  `Given`/`When`/`Then` stages rather than putting procedural setup and assertions
  directly in acceptance tests. These feature stages are an intended extension,
  not already implemented RLS APIs.
- `Given` describes domain preconditions, `When` describes the customer action,
  and `Then` describes observable outcomes. Prefer a readable behavioral story
  over a sequence of technical operations.
- Use feature-specific fluent builders with meaningful defaults and explicit
  overrides for behaviorally relevant values. Keep generated test data and
  incidental setup out of the scenario text.
- When scenarios need shared values, use a per-scenario context with logical
  aliases for generated identifiers and results. Resolve each parameter alias
  consistently, reject accidental result overwrites, and do not expose a failed
  result as a successful value. Add this support when needed, not as speculative
  framework scaffolding.
- For fluent behavior scenarios, collect setup and action descriptions first;
  defer asynchronous execution until the final assertion chain is awaited.
  Execute setup and the action once per scenario, even when several assertions
  observe the outcome.
- Extend channel-neutral driver contracts for new use cases and implement them
  through the applicable API/UI adapters. Keep channel selection in the existing
  test environment; scenarios must not branch on HTTP or browser mechanics.
- Drivers translate client outcomes into consistent asynchronous success/error
  results. Keep expected failures available for assertions rather than throwing
  before the scenario can verify them. Do not hide unexpected execution errors.
- Put outcome assertions in `Then`/verification helpers, including follow-up
  observations through drivers. Do not expose transport DTOs, status-code
  plumbing, selectors, or authentication-token mechanics in domain scenarios.
- Reuse the existing per-scenario scope and fixture composition. Extend the DSL,
  drivers, and clients together for each new behavior instead of copying the
  entire sandbox framework or introducing a parallel harness.

## Build and test

Do not add `.http` files. Cover API behavior through the existing acceptance-test
infrastructure instead.

Run commands from the repository root:

```sh
dotnet build src/DialDen.RLS.slnx
dotnet test src/tests/DialDen.RLS.Tests/DialDen.RLS.Tests.csproj
```

UI acceptance tests use Playwright and require its browser prerequisites. Preserve
the existing test configuration and hosting setup when running them.
