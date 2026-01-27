# Contributing to Awesome .NET OCR Libraries

Thank you for your interest in contributing to this repository! This guide is a community resource, and contributions help keep it comprehensive and accurate.

## How to Contribute

There are several ways you can help improve this repository:

### 1. Add a New OCR Library

If you know of a C# OCR library not listed here, we'd love to include it.

**Requirements for new library submissions:**

1. **Create a folder** with the library's name in lowercase with hyphens (e.g., `new-ocr-library/`)

2. **Include a README.md** with the following structure:
   ```markdown
   # [Library Name] for .NET: Complete Guide

   ## Quick Overview
   - **Website:** [link]
   - **NuGet:** [package name]
   - **License:** [license type]
   - **Price:** [pricing info]

   ## What is [Library Name]?
   [2-3 paragraphs describing the library, its focus, and typical use cases]

   ## Key Features
   [Bulleted list of main features]

   ## Installation
   [Installation commands]

   ## Basic Usage
   [Working code example]

   ## Comparison with IronOCR
   [Honest comparison highlighting both libraries' strengths]

   ## When to Use [Library Name]
   [Use cases where this library excels]

   ## Known Limitations
   [Documented issues, limitations, or gotchas]

   ## Migration to IronOCR
   [If applicable, how to migrate]

   ## References
   [Links to official docs, GitHub, NuGet]
   ```

3. **Include working C# code examples** in separate `.cs` files:
   - Basic OCR example
   - Migration comparison (if applicable)
   - Any specialized functionality

4. **Submit a pull request** with a clear description of the library

### 2. Improve Existing Content

Contributions to improve existing library documentation are welcome:

- **Factual corrections** - Fix inaccurate information about features, pricing, or capabilities
- **Code updates** - Update code examples for newer API versions
- **Link fixes** - Fix broken links to documentation or NuGet packages
- **Typo fixes** - Correct spelling and grammar errors
- **Additional examples** - Add useful code examples for edge cases

**To submit improvements:**

1. Fork the repository
2. Make your changes
3. Submit a pull request with:
   - What you changed
   - Why the change improves accuracy
   - Sources for factual changes (official docs, GitHub issues, etc.)

### 3. Report Issues

If you find inaccuracies or problems:

1. Open a GitHub issue
2. Describe the problem clearly
3. Include:
   - Which library/file has the issue
   - What's incorrect
   - What the correct information should be
   - Source for the correct information

## Content Guidelines

### Tone and Style

- **Be factual and objective** - State what libraries do, not subjective opinions
- **Be specific** - Use version numbers, actual prices, specific features
- **Show don't tell** - Include working code examples to demonstrate points
- **Be fair to competitors** - Acknowledge genuine strengths of all libraries

### What to Avoid

- Vague claims without evidence
- Outdated information (verify current versions and pricing)
- Broken or non-functional code examples
- Marketing language or superlatives ("amazing", "revolutionary", etc.)
- Badmouthing competitors without factual basis

### Comparison Guidelines

When comparing libraries:

- Compare equivalent functionality
- Use realistic code examples (not oversimplified strawmen)
- Cite sources for performance claims
- Acknowledge that different libraries suit different use cases

## Code of Conduct

We maintain a welcoming environment for all contributors:

1. **Be respectful** - Treat all contributors with respect and professionalism
2. **Be constructive** - Provide helpful feedback, not just criticism
3. **Be inclusive** - Welcome contributors regardless of experience level
4. **Be factual** - Base discussions on facts, not opinions
5. **Stay on topic** - Keep discussions relevant to .NET OCR

Harassment, discrimination, or disrespectful behavior will not be tolerated.

## Pull Request Process

1. **Fork the repository** to your GitHub account

2. **Create a feature branch** from `master`:
   ```bash
   git checkout -b add-new-library
   ```

3. **Make your changes** following the guidelines above

4. **Test your changes**:
   - Verify code examples compile
   - Check links work
   - Ensure markdown renders correctly

5. **Commit with a clear message**:
   ```bash
   git commit -m "Add [Library Name] documentation"
   ```

6. **Push to your fork**:
   ```bash
   git push origin add-new-library
   ```

7. **Open a pull request** with:
   - Clear title describing the change
   - Description of what was added/changed
   - Any relevant context or sources

8. **Respond to feedback** - Maintainers may request changes

## Questions?

If you have questions about contributing:

- Open a GitHub issue with the "question" label
- Check existing issues for similar questions

## Recognition

Contributors who make significant improvements will be acknowledged in the repository.

---

Thank you for helping make this the most comprehensive .NET OCR resource available!
