# GitHub Copilot Custom Instructions - Git Conventional Commit Messages

These instructions guide GitHub Copilot in generating Git commit messages that adhere to the Conventional Commits specification.

**I. Conventional Commits Specification:**

- "Generate commit messages that follow the Conventional Commits specification ([https://conventionalcommits.org/](https://conventionalcommits.org/))."
- "Structure commit messages with a type, an optional scope, and a description: `type(scope)?: description`."
- "Separate the header from the optional body and footer with a blank line."

**II. Commit Message Structure:**

- **Header:**
  - **Type:**
    - "Use one of the following types (in lowercase) based on these specific criteria:"
      - `feat`: **NEW FUNCTIONALITY** - Adding new features, endpoints, business logic, or capabilities that provide value to users. Examples:
        - Adding a new API endpoint
        - Implementing new business rules
        - Adding new authentication methods
        - Creating new domain entities or services
      - `fix`: **BUG RESOLUTION** - Correcting existing functionality that was not working as intended. Examples:
        - Fixing incorrect business logic
        - Resolving API response errors
        - Correcting data validation issues
        - Fixing security vulnerabilities
      - `refactor`: **CODE IMPROVEMENT WITHOUT BEHAVIOR CHANGE** - Restructuring existing code without changing its external behavior or adding new features. Examples:
        - Extracting methods or classes for better organization
        - Renaming variables/methods for clarity
        - Simplifying complex logic while maintaining same functionality
        - Moving code between files/namespaces for better architecture
        - Applying design patterns (Repository, Factory, etc.)
        - Converting synchronous code to asynchronous without adding features
      - `perf`: **PERFORMANCE OPTIMIZATION** - Code changes that specifically improve performance without adding new features. Examples:
        - Adding database indexes
        - Optimizing queries (Entity Framework)
        - Implementing caching mechanisms
        - Reducing memory allocations
        - Improving algorithm efficiency
      - `build`: Changes that affect the build system or external dependencies (e.g., NuGet packages, MSBuild, Docker).
      - `ci`: Changes to CI/CD configuration files and scripts (e.g., Azure Pipelines, GitHub Actions, YML files).
      - `docs`: Documentation only changes.
      - `style`: Changes that do not affect the meaning of the code (white-space, formatting, missing semicolons, etc.).
      - `test`: Adding missing tests or correcting existing tests.
      - `op`: Changes that affect operational components like infrastructure, deployment, backup, recovery, etc.
      - `chore`: Miscellaneous commits. Other changes that don't modify `src` or test files (e.g. .gitignore)
    - "**IMPORTANT**: If you're restructuring code without adding new functionality or changing behavior, use `refactor`, NOT `feat`."
    - "If none of the types apply, use 'chore'."
  - **Scope (Required for this project):**
    - "**ALWAYS include a scope** to provide context about what part of the codebase was affected."
    - "Use the following scope hierarchy (most specific applicable level):"
      - **Project/Service Level**: `Security.Api`, `Security.Application`, `Security.Domain`, `Security.Infrastructure`
      - **Layer/Folder Level**: `Security.Application.Features`, `Security.Application.Interfaces`, `Security.Infrastructure.Data`
      - **Feature/Component Level**: `auth`, `user-management`, `token-validation`, `database-migrations`
    - "**Scope Examples for .NET Microservices:**"
      - `feat(Security.Api): add user registration endpoint`
      - `fix(Security.Application): correct token validation logic`
      - `refactor(Security.Infrastructure): extract database repository pattern`
      - `perf(Security.Application.Features): optimize user query performance`
      - `test(Security.Application.UnitTests): add authentication service tests`
      - `docs(Security): update API documentation`
    - "If the change affects multiple projects/scopes, use the most general applicable scope or omit parentheses for cross-cutting changes."
  - **Description:**
    - "A concise description of the change in imperative, present tense (e.g., 'fix: correct typos in documentation', not 'fixed typos...')."
    - "Capitalize the first letter of the description."
    - "Do not end the description with a period."
    - "Limit the description to 50 characters."
- **Body (Optional):**
  - "Include a longer description of the changes, if necessary. Use complete sentences."
  - "Explain the motivation for the change."
  - "Wrap lines at 72 characters."
- **Footer (Optional):**
  - "Use the footer to reference issue trackers or breaking changes."
  - "**Breaking Changes:** Start with `BREAKING CHANGE: ` followed by a description of the breaking change."
  - "**Issue References:** Use `Closes #issueNumber`, `Fixes #issueNumber` or `Resolves #issueNumber` to link to issues."

**III. Commit Message Examples:**

**Basic Examples:**

- `feat(Security.Api): add user registration endpoint`
- `fix(Security.Application): correct token expiration validation`
- `refactor(Security.Infrastructure): extract repository pattern for user data`
- `perf(Security.Application): optimize user authentication query`
- `test(Security.Application.UnitTests): add token validation tests`
- `docs(Security): update authentication API documentation`

**Detailed Examples with Body:**

```
feat(Security.Api): add password reset functionality

Implement password reset endpoint with email verification.
Includes rate limiting and security validation.

Closes #125
```

```
refactor(Security.Infrastructure): reorganize database context structure

Extract entity configurations into separate files for better
maintainability. Move from single DbContext file to organized
configuration classes following Clean Architecture principles.
```

```
perf(Security.Application): implement caching for user queries

Add Redis caching layer for frequently accessed user data.
Reduces database calls by 80% for user authentication flows.
```

**Cross-Service Examples:**

- `feat: add distributed transaction support across services`
- `fix: resolve authentication token sharing between services`
- `refactor: standardize error handling across all microservices`

**IV. Instructions for Copilot:**

- "When generating commit messages, adhere strictly to the Conventional Commits specification."
- "**CRITICAL**: Distinguish carefully between `feat` and `refactor`:"
  - "Use `feat` ONLY when adding NEW functionality or capabilities"
  - "Use `refactor` when improving existing code structure without changing behavior"
  - "If restructuring code for better architecture = `refactor`"
  - "If adding new business logic or endpoints = `feat`"
- "**ALWAYS include a scope** using the project hierarchy: `ProjectName.LayerName` or `ProjectName.LayerName.FolderName`"
- "For .NET microservices, common scopes include:"
  - "`Security.Api`, `Security.Application`, `Security.Domain`, `Security.Infrastructure`"
  - "`Account.Api`, `Movement.Api`, `Transaction.Api`"
  - "`Security.Application.Features`, `Security.Infrastructure.Data`"
- "Use specific scopes like `auth`, `user-management`, `validation` for feature-level changes"
- "Write descriptions in imperative, present tense with capital first letter"
- "Limit header to 50 characters, body lines to 72 characters"
- "Add body for complex changes explaining motivation and context"
- "Use footer for breaking changes (`BREAKING CHANGE: `) and issue references (`Closes #123`)"
- "When in doubt between types, prefer the more specific type (e.g., `perf` over `refactor` for performance improvements)"

**V. .NET Microservices Specific Guidelines:**

**Common Scenarios and Correct Types:**

1. **Adding new functionality (`feat`):**

   - New API endpoints or controllers
   - New business logic or domain entities
   - New application features or commands/queries (CQRS)
   - New authentication/authorization features
   - New validation rules or business rules
   - New middleware or filters
   - New Entity Framework migrations with new features

2. **Code improvements without new features (`refactor`):**

   - Extracting methods or classes for better organization
   - Moving code between projects/namespaces
   - Applying design patterns (Repository, Factory, Strategy)
   - Converting synchronous to asynchronous without adding functionality
   - Simplifying complex code while maintaining same behavior
   - Reorganizing folder structure or file layout
   - Renaming for clarity without changing functionality
   - Consolidating duplicate code

3. **Performance improvements (`perf`):**

   - Database query optimizations
   - Adding indexes or improving Entity Framework queries
   - Implementing caching (Redis, Memory Cache, Response Caching)
   - Optimizing algorithms or data structures
   - Reducing memory allocations or improving resource usage
   - Implementing connection pooling
   - Optimizing serialization/deserialization

4. **Bug fixes (`fix`):**
   - Correcting incorrect business logic
   - Fixing API response issues or status codes
   - Resolving authentication/authorization bugs
   - Fixing data validation problems
   - Correcting configuration or dependency injection issues
   - Fixing null reference exceptions or error handling
   - Resolving Entity Framework mapping issues

**Scope Naming Conventions for this Project:**

- **Service Level**: `Security`, `Account`, `Movement`, `Transaction`
- **Project Level**: `Security.Api`, `Security.Application`, `Security.Domain`, `Security.Infrastructure`
- **Layer/Folder Level**: `Security.Application.Features`, `Security.Application.Interfaces`, `Security.Infrastructure.Data`, `Security.Infrastructure.Services`
- **Feature Level**: `auth`, `user-management`, `token-validation`, `password-reset`, `audit-logging`
- **Component Level**: `middleware`, `controllers`, `repositories`, `validators`, `handlers`

**Decision Tree for Commit Types:**

1. **Does it add new user-facing functionality?** → `feat`
2. **Does it fix broken functionality?** → `fix`
3. **Does it improve performance measurably?** → `perf`
4. **Does it change code structure without changing behavior?** → `refactor`
5. **Does it add/modify tests only?** → `test`
6. **Does it change documentation only?** → `docs`
7. **Everything else** → `chore`
