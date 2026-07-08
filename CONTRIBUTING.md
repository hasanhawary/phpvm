# Contributing to PVM (PHP Version Manager)

First off, thank you for considering contributing to `pvm`! PVM is engineered to be a production-grade, enterprise-ready reference tool for Windows developers.

## 🏛️ Architecture & Engineering Standards

PVM strictly adheres to **Clean Architecture**, **SOLID principles**, and **Domain-Driven Design (DDD)**:

1. **Layer Dependency Rules**:
   - `Pvm.Core`: Pure domain layer. Contains only value objects, domain entities, enums (`Pvm.Core.Enums`), and interface ports (`Pvm.Core.Ports`). **Zero infrastructure or UI dependencies.**
   - `Pvm.Application`: Orchestration layer. Implements use cases and services (`SwitchService`, `DoctorService`, etc.) by depending only on `Pvm.Core`.
   - `Pvm.Infrastructure`: Implementation layer. Implements filesystem junctions, registry PATH access, HTTP scraping, ZIP extraction, and JSON configuration.
   - `Pvm.Cli`: Presentation layer. Uses `Spectre.Console` for UI rendering and command routing.

2. **No Exception Control Flow**:
   - Do **NOT** use `try/catch` for expected domain or logic flow.
   - All operations must return explicit `Result` or `Result<T>` monads.

3. **Native AOT & Trimming Compatibility**:
   - PVM is published as a single-file, trimmed, self-contained executable.
   - **Never** use reflection-based JSON serialization (`JsonSerializer.Serialize` without context).
   - Always use source-generated JSON via `PvmJsonSerializerContext`.

4. **Architecture Tests**:
   - Any architectural violation (e.g., placing enums outside `Pvm.Core.Enums` or referencing UI from Core) will cause `Pvm.Architecture.Tests` to fail during automated CI builds.

---

## 🛠️ Development Setup

1. **Prerequisites**:
   - Windows 10/11 (x64)
   - .NET 8.0 SDK or later
   - PowerShell 5.1 or PowerShell 7+

2. **Building & Testing Locally**:
   You can use our automated build script:
   ```powershell
   # Build, run all 55 unit tests, and publish pvm.exe to ./dist
   .\build.ps1
   
   # Perform a clean build
   .\build.ps1 -Clean
   ```
   Or using the .NET CLI:
   ```powershell
   dotnet restore
   dotnet build -c Release
   dotnet test -c Release
   ```

---

## 📬 Submitting Pull Requests

1. Create a feature branch from `main` (e.g., `feature/add-new-diagnostic-check`).
2. Ensure all new features or bug fixes are covered by unit tests in the appropriate test suite (`Pvm.Core.Tests`, `Pvm.Application.Tests`, etc.).
3. Run `.\build.ps1` and ensure **all unit tests pass** and zero build/trimming warnings occur.
4. Submit a Pull Request with a clear summary of your changes and why they were made.
