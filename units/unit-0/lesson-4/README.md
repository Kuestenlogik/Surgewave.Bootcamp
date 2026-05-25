# Lesson 0.4: Contributing to Surgewave

> **Difficulty:** Beginner | **Duration:** 20 min | **Prerequisites:** None

## Overview

Learn how to contribute to the Surgewave project, report bugs, and submit improvements. Surgewave is an open-source project and thrives on community contributions.

## Learning Objectives

- Understand the Surgewave project structure on GitHub
- Learn how to report bugs effectively
- Know how to submit feature requests
- Contribute code via pull requests
- Follow contribution guidelines and code standards

## GitHub Repository

Surgewave is hosted on GitHub:

```
https://github.com/Kuestenlogik/Surgewave
```

### Repository Structure

```
Surgewave/
├── src/                    # Source code
│   ├── Kuestenlogik.Surgewave.Runtime/   # Core runtime
│   ├── Kuestenlogik.Surgewave.Client/    # Client library
│   ├── Kuestenlogik.Surgewave.Streams/   # Stream processing
│   └── ...
├── tests/                  # Test projects
├── docs/                   # Documentation
├── samples/                # Example applications
├── bootcamp/               # This bootcamp!
└── deploy/                 # Deployment configs
```

## Reporting Bugs

### Before Reporting

1. **Search existing issues** - Your bug may already be reported
2. **Check the documentation** - The solution might be documented
3. **Try the latest version** - The bug may be fixed

### Creating a Bug Report

Use the GitHub issue template:

```markdown
## Bug Description
A clear description of what the bug is.

## Steps to Reproduce
1. Configure Surgewave with...
2. Run the following code...
3. Observe the error...

## Expected Behavior
What you expected to happen.

## Actual Behavior
What actually happened.

## Environment
- Surgewave Version: x.y.z
- .NET Version: 10.0
- OS: Windows/Linux/macOS
- Configuration: (relevant settings)

## Logs/Stack Trace
```
Paste relevant logs here
```

## Additional Context
Any other relevant information.
```

### Good Bug Report Example

> **Title:** Consumer throws NullReferenceException when topic doesn't exist
>
> **Description:** When subscribing to a non-existent topic with AutoCreateTopics=false, the consumer throws an unhandled NullReferenceException instead of a descriptive error.
>
> **Steps:** (detailed reproduction steps)

## Feature Requests

### Submitting Ideas

1. **Check the roadmap** - Review `ROADMAP.md` for planned features
2. **Search discussions** - Your idea may already be discussed
3. **Create an issue** with the "enhancement" label

### Feature Request Template

```markdown
## Feature Description
What feature would you like?

## Use Case
Why do you need this feature? What problem does it solve?

## Proposed Solution
How do you envision this working?

## Alternatives Considered
What alternatives have you considered?

## Additional Context
Mockups, examples from other systems, etc.
```

## Contributing Code

### Getting Started

```bash
# 1. Fork the repository on GitHub

# 2. Clone your fork
git clone https://github.com/YOUR-USERNAME/Surgewave.git
cd Surgewave

# 3. Add upstream remote
git remote add upstream https://github.com/Kuestenlogik/Surgewave.git

# 4. Create a feature branch
git checkout -b feature/my-improvement

# 5. Build and test
dotnet build Kuestenlogik.Surgewave.slnx
dotnet test
```

### Making Changes

1. **Follow code style** - Match existing patterns
2. **Add tests** - New features need tests
3. **Update docs** - Document your changes
4. **Keep commits focused** - One logical change per commit

### Commit Message Format

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types:** `feat`, `fix`, `docs`, `test`, `refactor`, `perf`, `chore`

**Examples:**
```
feat(client): add batch offset commit support

fix(streams): resolve window aggregation memory leak

docs(bootcamp): add Unit 22 AI/LLM lessons
```

### Submitting a Pull Request

```bash
# 1. Push your branch
git push origin feature/my-improvement

# 2. Create PR on GitHub
# - Use descriptive title
# - Reference related issues
# - Describe your changes
# - Add screenshots if UI changes
```

### Pull Request Template

```markdown
## Description
What does this PR do?

## Related Issues
Fixes #123
Related to #456

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
How did you test this?

## Checklist
- [ ] Code follows project style
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] CHANGELOG updated (if applicable)
```

## Code Review Process

1. **Automated checks** - CI must pass
2. **Maintainer review** - Code review by core team
3. **Address feedback** - Make requested changes
4. **Approval & merge** - Maintainer merges when ready

## Documentation Contributions

Documentation improvements are always welcome:

- Fix typos and grammar
- Clarify confusing sections
- Add missing examples
- Translate documentation

```bash
# Documentation is in docs/ folder
cd docs
# Edit markdown files
# Submit PR as usual
```

## Bootcamp Contributions

Help improve this bootcamp:

- Fix errors in lessons
- Add clarifying examples
- Suggest new lessons or units
- Improve exercises

## Code of Conduct

All contributors must follow the [Code of Conduct](https://github.com/Kuestenlogik/Surgewave/blob/main/CODE_OF_CONDUCT.md):

- Be respectful and inclusive
- Welcome newcomers
- Accept constructive criticism
- Focus on what's best for the community

## Recognition

Contributors are recognized in:

- `CONTRIBUTORS.md` file
- Release notes
- Project documentation

## Key Takeaways

- Surgewave is open source and welcomes contributions
- Report bugs with detailed reproduction steps
- Feature requests should include use cases
- Follow the contribution guidelines for code
- Documentation improvements are valuable contributions

---

**Next:** [Lesson 0.5: Getting Support](../lesson-5/README.md)
