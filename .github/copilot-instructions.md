# GitHub Copilot Instructions

## Preferred Workflow

- We follow Test-Driven Development (TDD):
  - Write tests before implementing features.
  - Ensure all code is covered by meaningful, maintainable tests.
- We use Domain Driven Design (DDD):
  - Focus on the core domain and domain logic.
  - Use ubiquitous language and clear domain boundaries.
- We prefer C# idioms and clean code:
  - Use expressive naming, clear intent, and avoid unnecessary complexity.
  - Follow SOLID principles and keep methods/classes small and focused.
  - Use async/await for asynchronous code.
  - Prefer immutability and value objects where appropriate.
- **After making any changes, always run the tests to verify that nothing is broken.**

## Documentation Guidelines

- Write XML documentation comments for all public APIs, classes, and important methods in C#.
- Ensure documentation is clear, concise, and describes intent, parameters, return values, and exceptions.
- For complex logic, include remarks or examples to aid understanding.
- Keep documentation up to date with code changes.
- For non-C# code, use appropriate docstring or comment conventions (e.g., JSDoc for JavaScript/TypeScript).
- Document domain concepts and boundaries clearly, following DDD principles.

## Project Context
This is a language learning tools project. When suggesting code:
- Prioritize accessibility features
- Include proper error handling for API calls
- Use semantic HTML elements
- Follow responsive design principles




