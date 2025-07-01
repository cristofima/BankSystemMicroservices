# GitHub Copilot Custom Instructions - Git Conventional Commit Messages

These instructions guide GitHub Copilot in generating Git commit messages that adhere to the Conventional Commits specification.

**I. Conventional Commits Specification:**

- "Generate commit messages that follow the Conventional Commits specification ([https://conventionalcommits.org/](https://conventionalcommits.org/))."
- "Structure commit messages with a type, an optional scope, and a description: `type(scope)?: description`."
- "Separate the header from the optional body and footer with a blank line."

**II. Commit Message Structure:**

- **Header:**
  - **Type:**
    - "Use one of the following types (in lowercase):"
      - `feat`: A new feature for the user, not a feature for the build process.
      - `fix`: A bug fix for the user, not a fix to a build script.
      - `build`: Changes that affect the build system or external dependencies (e.g., npm, webpack).
      - `ci`: Changes to our CI configuration files and scripts (e.g., Travis, Circle, BrowserStack).
      - `docs`: Documentation only changes.
      - `perf`: A code change that improves performance.
      - `refactor`: A code change that neither fixes a bug nor adds a feature.
      - `style`: Changes that do not affect the meaning of the code (white-space, formatting, missing semicolons, etc.).
      - `test`: Adding missing tests or correcting existing tests.
      - `test`: Adding missing tests or correcting existing tests.
      - `op` : Changes that affect operational components like infrastructure, deployment, backup, recovery, etc.
      - `chore`: Miscellaneous commits. Other changes that don't modify `src` or test files (e.g. .gitignore)
    - "If none of the types apply, use 'chore'."
  - **Scope (Optional):**
    - "An optional scope to provide additional context (e.g., component name, module name). Enclose the scope in parentheses: `(scope)`."
    - "If the change affects multiple scopes, omit the scope."
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

- "fix: correct typos in documentation"
- "feat(user): add user profile page"
- "feat: add new user authentication method"
- "fix(auth): handle expired tokens correctly"
- "chore: update dependencies"
- "docs: update API documentation"
- "refactor(components): simplify button component"
- "test: add unit tests for user service"
- "BREAKING CHANGE: remove support for Node 12"
- "Closes #123"

**IV. Instructions for Copilot:**

- "When generating commit messages, adhere strictly to the Conventional Commits specification."
- "Use the appropriate type from the list above."
- "Include a scope if the change is specific to a component or module."
- "Write a clear and concise description in the imperative, present tense."
- "Add a body if the change requires more explanation."
- "Use the footer for breaking changes and issue references."
- "Prioritize clarity and conciseness in the commit message."
