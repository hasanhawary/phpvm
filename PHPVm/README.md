# pvm — PHP Version Manager for Windows

A production-grade, fast, and zero-friction PHP version manager for Windows, inspired by nvm, fnm, pyenv, rustup, and sdkman.

## Overview

`pvm` allows Windows developers to install, manage, and switch between multiple PHP versions without manually editing PATH variables or requiring administrator privileges. It utilizes native NTFS junctions (`mklink /J`) for transparent, instant version switching across all terminal sessions.

## Quick Start

```powershell
# List installed PHP versions
pvm list

# Switch active PHP version
pvm use 8.3

# Check active version
pvm current

# Install a new PHP version
pvm install 8.4

# Run diagnostics
pvm doctor
```

## Architecture

Built with **.NET 8** following strict **Clean Architecture**, **SOLID**, and **Domain-Driven Design** principles:
- **Pvm.Core**: Pure domain models, value objects, enums, and port interfaces (zero infrastructure dependencies).
- **Pvm.Application**: Use case orchestrators, version resolution, and diagnostics engine.
- **Pvm.Infrastructure**: Windows APIs (NTFS junctions, Registry PATH), HTTP download clients, ZIP extraction, and JSON configuration.
- **Pvm.Cli**: Presentation layer powered by `Spectre.Console`.

## License

MIT License
