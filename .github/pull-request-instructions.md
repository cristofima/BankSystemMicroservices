# GitHub Copilot Pull Request Instructions

> **Note**: For detailed Git commit message conventions and comprehensive guidance, see [git-commit-messages-instructions.md](./git-commit-messages-instructions.md). This file focuses specifically on Pull Request titles and descriptions.

## Pull Request Title Guidelines

When generating pull request titles, follow these enhanced conventions for better GitHub Copilot integration:

### Format

Use the format: `type(scope): brief description of changes`

- **type**: Use lowercase conventional commit types
- **scope**: Include specific project/service scope (required for this project)
- **description**: Imperative, present tense, capitalize first letter, no period

### Types (Lowercase for PR Titles)

- **feat**: New feature implementation or significant functionality additions
- **fix**: Bug fixes and error corrections
- **refactor**: Code refactoring without functional changes (structural improvements)
- **docs**: Documentation updates and improvements
- **test**: Adding or updating tests, test infrastructure
- **chore**: Maintenance tasks, dependency updates, tooling changes
- **perf**: Performance improvements and optimizations
- **security**: Security-related changes and fixes
- **ci**: Continuous integration and deployment changes
- **build**: Build system changes, dependency management
- **style**: Formatting changes, code style improvements

### Scope Guidelines

Always include a scope to provide context about the affected area:

- **Service Level**: `Security`, `Account`, `Movement`, `Transaction`, `Notification`, `Reporting`
- **Project Level**: `Security.Api`, `Security.Application`, `Security.Domain`, `Security.Infrastructure`
- **Cross-cutting**: `shared`, `gateway`, `infrastructure`, `docs`

### Examples

- `feat(Security.Api): add user authentication service with JWT support`
- `fix(Account.Application): resolve account balance calculation error in transaction processing`
- `refactor(Security.Infrastructure): restructure payment domain models following DDD patterns`
- `docs(Security): update API documentation for transaction endpoints`
- `test(Account.Application.UnitTests): add unit tests for account validation logic`
- `chore(shared): update Entity Framework Core to version 9.0`
- `perf(Transaction.Application): optimize transaction query performance with caching`
- `security(Security.Api): implement rate limiting for authentication endpoints`

### PR Title Best Practices for GitHub Copilot

1. **Be Specific**: Include the main component or feature affected
2. **Use Active Voice**: Describe what the PR does, not what was done
3. **Limit Length**: Keep titles under 60 characters for better GitHub UI display
4. **Focus on Value**: Emphasize the business or technical value delivered
5. **Reference Architecture**: Mention if changes follow Clean Architecture, DDD, or CQRS patterns

## Pull Request Description Template

When generating pull request descriptions, GitHub Copilot should include the following sections for comprehensive documentation:

### Enhanced Description Format for GitHub Copilot

```markdown
## Summary

Brief overview of what this PR accomplishes and why it's needed. Focus on business value and technical impact.

## Type of Change

- [ ] 🆕 New feature (non-breaking change which adds functionality)
- [ ] 🐛 Bug fix (non-breaking change which fixes an issue)
- [ ] ♻️ Refactoring (code change that neither fixes a bug nor adds a feature)
- [ ] 📚 Documentation update
- [ ] 🧪 Test improvement
- [ ] ⚡ Performance improvement
- [ ] 🔒 Security enhancement
- [ ] 💔 Breaking change (fix or feature that would cause existing functionality to not work as expected)

## Changes Made

### Core Changes

- List the main changes implemented
- Include any new features or components added
- Mention any files or modules modified

### Architecture Impact

- Note if changes follow Clean Architecture principles
- Reference DDD patterns applied (Aggregates, Entities, Value Objects, Domain Services)
- Specify CQRS implementation details (Commands, Queries, Handlers)
- Mention any design patterns implemented (Repository, Factory, Strategy, etc.)

### Breaking Changes (if applicable)

- Detail any breaking changes to API contracts
- Note database schema changes
- Mention configuration changes required

## Related Issues

- Closes #[issue-number]
- Relates to #[issue-number]
- Addresses requirement from [specification/document]

## Testing

### Automated Testing

- [ ] Unit tests added/updated (target: >80% coverage)
- [ ] Integration tests added/updated
- [ ] End-to-end tests added/updated (if applicable)
- [ ] All existing tests pass
- [ ] New tests follow AAA pattern (Arrange, Act, Assert)

### Manual Testing

- [ ] Manual testing completed
- [ ] Edge cases tested
- [ ] Error scenarios validated
- [ ] Performance testing conducted (if applicable)

### Test Coverage

- Current coverage: XX%
- Coverage change: +/- XX%
- Critical paths covered: [list critical business logic tested]

## Code Quality & Architecture

### Code Standards

- [ ] Code follows project conventions and guidelines
- [ ] No new code analysis warnings
- [ ] Follows Clean Code principles
- [ ] SOLID principles applied appropriately
- [ ] Naming conventions followed

### Architecture Compliance

- [ ] Changes follow Clean Architecture structure
- [ ] Domain-Driven Design patterns applied correctly
- [ ] CQRS boundaries respected (Commands vs Queries)
- [ ] Dependency injection used properly
- [ ] No inappropriate cross-layer dependencies

### Performance & Security

- [ ] Performance impact considered and measured
- [ ] Security implications reviewed
- [ ] No sensitive data exposure
- [ ] Input validation implemented
- [ ] Authentication/authorization considered

## Deployment Notes

### Database Changes

- [ ] Database migrations included
- [ ] Migration scripts tested
- [ ] Rollback strategy defined
- [ ] Data seeding requirements documented

### Configuration Changes

- [ ] New configuration settings documented
- [ ] Environment-specific settings noted
- [ ] Azure Key Vault secrets added (if applicable)
- [ ] Feature flags configured (if applicable)

### Infrastructure Impact

- [ ] No infrastructure changes required
- [ ] Docker configuration updated
- [ ] Azure resources modified
- [ ] Service dependencies updated

### Monitoring & Observability

- [ ] Logging added for new functionality
- [ ] Metrics/telemetry implemented
- [ ] Health checks updated
- [ ] Application Insights integration verified

## Screenshots/Demo

Include screenshots, GIFs, or demo links if the changes include UI modifications or new visual features.
```

## Specific Guidelines for Bank System Microservices

### Domain Context

When describing changes, reference the appropriate bounded context:

- **Security Service**: Authentication, authorization, user management
- **Account Service**: Account creation, management, customer data
- **Transaction Service**: Payment processing, transaction history
- **Movement Service**: Money transfers, account movements
- **Notification Service**: Email, SMS, push notifications
- **Reporting Service**: Financial reports, analytics

### Technical Context

- Mention if changes follow Clean Architecture principles
- Reference DDD patterns when applicable (Aggregates, Entities, Value Objects)
- Note CQRS implementation details (Commands vs Queries)
- Specify if changes affect API contracts or database schema
- Include performance implications for high-volume operations

### Security Considerations

- Always mention security implications
- Note any changes to authentication/authorization
- Specify if sensitive data handling is involved
- Include compliance considerations (PCI DSS, data protection)

### Example Domain-Specific Description

```markdown
## Summary

Implements the new account creation workflow in the Account Service, following Clean Architecture and DDD patterns. This change introduces proper validation, domain events, and audit logging for account creation operations.

## Changes Made

- Added `CreateAccountCommand` and `CreateAccountCommandHandler` in Application layer
- Implemented `Account` aggregate root with business validation rules
- Created `AccountCreatedEvent` domain event for eventual consistency
- Added account number generation strategy with uniqueness validation
- Integrated with Security Service for customer identity verification

## Domain Impact

- **Bounded Context**: Account Management
- **Aggregate**: Account (new aggregate root)
- **Events**: AccountCreatedEvent published to event bus
- **Integration**: Security Service for customer validation

## Database Changes

- New migration: `20250805_AddAccountsTable`
- Indexes added for account number uniqueness and customer lookups
- Audit fields included for compliance tracking
```

## Language and Tone for Pull Requests

- Use clear, professional English
- Be specific about technical implementations
- Use present tense for describing what the PR does
- Use past tense for describing the problem being solved
- Avoid jargon without explanation
- Include relevant technical details for reviewers

## Code Review Facilitation

Include information that helps reviewers:

- Highlight complex logic or business rules
- Explain architectural decisions made
- Point out areas that need special attention
- Reference relevant documentation or specifications
- Mention any trade-offs or alternative approaches considered

## GitHub Copilot Optimization Guidelines

### For Better PR Title Generation

When GitHub Copilot generates PR titles, it should:

1. **Analyze the commit history** to understand the scope and type of changes
2. **Identify the primary service/project** affected for accurate scoping
3. **Determine the change type** based on the actual modifications (feat vs refactor vs fix)
4. **Use conventional commit format** with lowercase type and proper scope
5. **Keep titles concise** but descriptive (under 60 characters)

### For Enhanced PR Description Generation

GitHub Copilot should:

1. **Scan changed files** to understand architectural layers affected
2. **Identify patterns** like new Commands/Queries, Entities, Controllers, etc.
3. **Detect breaking changes** by analyzing API changes and database migrations
4. **Reference domain concepts** relevant to the banking system
5. **Include specific technical details** about DDD, Clean Architecture, and CQRS implementations
6. **Suggest appropriate testing strategies** based on the type of changes
7. **Consider security implications** for financial system changes

### Context Keywords for Copilot

Include these keywords in descriptions when applicable:

- **Domain**: Aggregate, Entity, Value Object, Domain Service, Domain Event
- **Application**: Command, Query, Handler, DTO, Validator, Mapper
- **Infrastructure**: Repository, DbContext, Migration, External Service
- **API**: Controller, Middleware, Filter, Model Binding, Response
- **Security**: Authentication, Authorization, JWT, Rate Limiting, Encryption
- **Performance**: Caching, Indexing, Query Optimization, Connection Pooling
- **Integration**: Event Bus, Message Queue, External API, Service Communication

### Commit Message Reference

For detailed commit message conventions and examples specific to this project, always refer to:
📋 **[git-commit-messages-instructions.md](./git-commit-messages-instructions.md)**

This file contains:

- Conventional Commits specification implementation
- Detailed type definitions with banking domain examples
- Scope hierarchy for microservices architecture
- Decision trees for choosing correct commit types
- .NET-specific scenarios and patterns
