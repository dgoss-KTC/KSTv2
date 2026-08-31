## Local Coding Agent Workflow

KST v2 currently uses OpenCode with Magic Context and AFT active for local agentic development.
This is not an architectural dependency; a different development environment may be used later if it
follows the same repository, security, and human-control requirements.

When preparing implementation work for the coding agent:

- Assume the repository and its `AGENTS.md` are the coding agent's authoritative persistent project context.
- Prefer smaller, clearly bounded implementation checkpoints over large multi-purpose agent prompts.
- Separate repository investigation/planning from implementation when the task is complex, cross-layer, or contains unresolved assumptions.
- Design prompts for the workflow: **Explore → Plan → Human Review → Implement → Verify → Review**.
- Encourage the coding agent to retrieve relevant repository documentation rather than embedding large amounts of existing documentation into every prompt.
- State business rules, newly discovered source mappings, acceptance criteria, and task-specific constraints explicitly when they are not yet present in authoritative repository documentation.
- Do not assume the local model will reliably infer architectural constraints or recover prior conversational decisions unless those decisions are represented in the repository.
- Prefer structured clarification over allowing the coding agent to guess when requirements, QAD mappings, or business rules are uncertain.
- Use automated tests, compiler/type checks, repository architecture tests, and manual guided verification as external correctness mechanisms.
- Treat Magic Context, AFT, OpenCode session context, model memory, and other retrieval/context mechanisms as assistance only; repository documentation, accepted implementation, and project-owner decisions remain authoritative.
- Use parallel or subagent work only when decomposition provides clear value. Avoid unnecessary concurrent long-context work when it mainly duplicates repository reading or increases local context/compute pressure; prefer serial bounded work in those cases.
- When preparing prompts, optimize for correctness and efficient context use rather than maximizing model reasoning or context consumption.

The local-agent transition does not change KST v2 architecture, business requirements, safety boundaries, stage structure, or acceptance standards.

Current Magic Context and AFT capabilities are already active. Do not silently install or activate
additional plugins, extensions, packages, skills, MCP servers, agent modes, or other capabilities.
When a task genuinely requires an additional capability, surface it to the project owner first and
follow the enacted admission and security requirements.

Security: human control over additional capability installation or activation is a specific case of the
repository's general security requirement that new development-agent tooling is not autonomously
installed or activated — see `SECURITY.md` and `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`.
