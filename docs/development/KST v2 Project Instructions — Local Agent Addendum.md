## Local Coding Agent Workflow

KST v2 implementation may use a local Qwen3.8-27B coding model through Pi rather than a frontier cloud coding agent.

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
- Treat agent memory as retrieval assistance only; repository documentation and accepted implementation remain authoritative.
- Reserve unusually large or complex agent tasks for cases where decomposition would materially harm coherence.
- When preparing prompts, optimize for correctness and efficient context use rather than maximizing model reasoning or context consumption.

The local-agent transition does not change KST v2 architecture, business requirements, safety boundaries, stage structure, or acceptance standards.

Installed package invocation: Do not assume the coding agent can autonomously invoke installed Pi packages or modes. When a task benefits from a specific installed package, identify the package explicitly and instruct the project owner to activate it before submitting the agent prompt. Treat package activation as part of the human-controlled workflow.
