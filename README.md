# SBOM CLI

## Background

Your team maintains a library of Software Bill of Materials (SBOMs) from various applications.
Each SBOM lists software components, versions, and licenses. You are asked to build a simple
CLI tool (in the language of your choice) that:

1. Ingests SBOMs into a database.
2. Allows queries to find documents or packages containing a specific component (optionally filtered by version) or a
   specific license.

## Requirements

### CLI Functionality

#### Ingest SBOM

```
sbom-cli ingest <sbom-file>
```

- Parse a JSON SBOM (CycloneDX 1.6 or SPDX 3.0 format is acceptable).
- Store in a local database (any data store of your choosing is fine including in-memory, it
  doesn't have to be a production-quality database).

#### Query SBOMs

```
sbom-cli query --component <name> [--version <version>]
sbom-cli query --license <license>
```

- Returns all documents or packages that contain the specified component.
    - If `--version` is provided, only match that version.
- Returns all documents or packages that contain the specified license.

## Running the Project

### Setup

1. Install [mise](https://mise.jdx.dev/getting-started.html) and
   [activate it in your shell](https://mise.jdx.dev/getting-started.html#activate-mise)
   (e.g. `echo 'eval "$(mise activate zsh)"' >> ~/.zshrc`).
2. From the repository root, trust the project config and install the .NET SDK pinned in `mise.toml`:
   ```
   mise trust
   mise install
   ```
3. Build the project:
   ```
   dotnet build
   ```

If you'd rather not activate mise, prefix each `dotnet` command with `mise exec --`.

### Usage

Run commands from the repository root with `dotnet run --` in place of `sbom-cli`. The `--` is required so
options like `--version` are passed to the CLI rather than to `dotnet`. The database is stored in
`sbom-repository.db` in the current directory.

Reset the database and load the example SBOMs:

```
dotnet run -- init-db
dotnet run -- ingest examples/application-1.json
dotnet run -- ingest examples/application-2.json
dotnet run -- ingest examples/application-3.json
```

Query the loaded SBOMs:

```
# Component present in all three examples
dotnet run -- query --component "Example Application"

# Component in application-2 only
dotnet run -- query --component tomcat-catalina-2

# Filtered by version: the first matches, the second returns no results
dotnet run -- query --component tomcat-catalina-2 --version 9.0.15
dotnet run -- query --component tomcat-catalina-2 --version 9.0.14

# SPDX license id: matches application-1 and application-2
dotnet run -- query --license Apache-2.0

# License name (for licenses without an SPDX id): matches all three examples
dotnet run -- query --license "Acme Proprietary License"

# SPDX expression: matches application-3
dotnet run -- query --license "Apache-2.0 OR MIT"
```

Expressions are matched exactly, so `--license MIT` returns no results even though application-3 offers MIT
as an option.

Run `dotnet run -- --help` or `dotnet run -- query --help` for all options.

## AI Usage

I coded the vast majority of this by hand, but I did use AI at the end to help me get over the finish line.

- Gemini
    - Researching simple document stores similar to MongoDB.
    - Researching libraries to parse/validate SBOM document formats.
- Claude Code
    - Troubleshooting LiteDb query syntax nuances since my preferred LINQ approach didn't seem to
      support the level of nesting the query needed.
    - Cleaning up argument parsing and updating test cases to adequately exercise functionality.
    - Updating README with steps on how to run the project

## Additional Feature Ideas

- Validate the document against the SBOM format before ingesting
- Support both SBOM format types
- Enhance the CLI to support more types of queries with more flexibility
- Exploring other features of the database related to scale and performance, including bulk loading data and
  benchmarking query performance
