# ADR-0008: The operations copilot on Microsoft Agent Framework, with a deterministic demo provider

- Status: Accepted
- Date: 2026-11-03

## Context
The master prompt (§5) asks for an operations copilot that answers from platform data through real tools, has a
provider abstraction, audit, retries and timeouts, token and cost tracking, works without Azure AI credentials, and
never passes simulated answers off as model output. It names Semantic Kernel or Microsoft Agent Framework and asks
for no redundant frameworks. The repository is public and CI has no AI credentials.

## Decision
- **Microsoft Agent Framework** (`Microsoft.Agents.AI` 1.26.0, stable) on **`Microsoft.Extensions.AI`** 10.11.0.
  Agent Framework is the successor Microsoft recommends for new agent work over Semantic Kernel's agent APIs, and it
  builds on `Microsoft.Extensions.AI` abstractions (`IChatClient`, `AIFunction`) that the rest of .NET already uses.
  Version 1.27.0 was four days old when this was decided; 1.26.0 is pinned until a deliberate upgrade.
- **Provider abstraction = `IChatClient`.** Two implementations are registered by configuration:
  - an OpenAI-compatible client (`Microsoft.Extensions.AI.OpenAI` 10.11.0 over the `OpenAI` 2.14.0 SDK), which covers
    OpenAI and Azure AI Foundry's OpenAI v1 endpoint with one package;
  - a **deterministic demo provider**, used whenever no endpoint is configured. It is an `IChatClient`, so it runs
    through the same `ChatClientAgent` and function-invocation loop as a real model: it requests tools with fixed
    rules, receives their results from the framework, and composes the answer only from those results. A spike on
    2026-11-03 confirmed that `ChatClientAgent` drives a custom `IChatClient` through tool calls and aggregates usage.
- **Tools are typed HTTP clients to the services**, carrying the caller's bearer token. The services stay the only
  authority on what a user may see; AiOperations defines its own response records and references no service project.
- **AiOperations owns a database** (`logistiflow_aiops`) for conversations and the audit trail (questions, answers,
  tool calls, token usage, estimated cost). Tokens and secrets are never written to it.
- **Read-only first.** Tools only read. Actions that change state are a later spec with preview, confirmation,
  idempotency and audit (§5.3).

## Consequences
- The whole agent path (tool selection, invocation, audit, UI) is tested in CI with the demo provider; only the
  quality of a real model's wording is not.
- Answers are labelled with `provider`, `model` and `simulated`, so a demo can never be mistaken for a model.
- Swapping in Azure OpenAI, OpenAI or a local OpenAI-compatible server is configuration, not code.
- Agent Framework moves quickly; the pinned version and this spike are the baseline for upgrades.

## Alternatives considered
- **Semantic Kernel**: mature, but its agent APIs are being folded into Agent Framework; two frameworks would be
  redundant.
- **Calling a model SDK directly**: less code at first, but the tool loop, usage aggregation and provider switching
  would be rewritten by hand.
- **A demo mode that returns canned text**: rejected; it would not exercise the tools and would show answers not
  built from data.
