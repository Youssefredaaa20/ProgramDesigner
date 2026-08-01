# Program Designer API

## 1. Overview
The Program Designer API is a REST API designed to model nested education programs, allowing administrators to define structured curricula. It supports creating hierarchical trees composed of `Step` nodes (individual tasks like attending a session or passing a test) and `Group` nodes (collections of nodes that must be completed `InOrder` or as a `Choice`). The API handles complex logic for resolving node prerequisites across the tree and provides a validation engine to detect impossible requirements, such as circular dependencies or unreachable prerequisites hidden within choice structures.

## 2. Setup Instructions
To run this project from a completely clean clone, follow these steps:

**Prerequisites:**
- **.NET SDK:** Target framework is **.NET 9.0** (`net9.0`).
- **Database:** **SQL Server LocalDB** or **SQLEXPRESS** (not SQLite). Make sure your SQL Server instance is running. The default connection string in `appsettings.json` expects `DESKTOP-QEDOLN5\SQLEXPRESS01` — **this is machine-specific**. Update it to match your local SQL Server instance name before running (e.g. `(localdb)\mssqllocaldb` if you're using LocalDB instead of a named SQLEXPRESS instance).

**Commands:**
Run the following commands in the root of the repository in order:
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Apply EF Core migrations to create the database schema
dotnet ef database update --project ProgramDesigner.Infrastructure --startup-project ProgramDesigner.Api

# Run the API
dotnet run --project ProgramDesigner.Api
```

**Accessing API Documentation:**
Once the API is running, you can access the interactive Scalar/OpenAPI documentation at:
`http://localhost:5169/scalar/v1`

## 3. Data Model Explanation
The domain model uses an abstract base class `ProgramNode` with two concrete implementations: `StepNode` and `GroupNode`.

Entity Framework Core stores this using the **TPH (Table-per-Hierarchy)** approach. All nodes are stored in a single `ProgramNodes` table, with a discriminator column `NodeType` ("Step" or "Group") differentiating the specific types.

**Base properties (`ProgramNode`):**
- `Id` (Guid): The unique identifier for the node. For the root node of a program, this value is reused as the program's own identifier (see trade-off notes below).
- `ProgramId` (Guid): The identifier grouping all nodes of a single program tree together.
- `ParentId` (Guid?): Builds the hierarchical tree structure (null for the root node).
- `OrderIndex` (int): Maintains the specific sequential ordering of children within a group.
- `Name` (string): The human-readable name of the node.
- `PrerequisiteId` (Guid?): Defines dependencies. Distinct from `ParentId`, this points to another node in the tree that must be completed before this node becomes available.

**Concrete properties:**
- **`StepNode`**: Represents a leaf node. Contains `Type` (`StepType` enum: `AttendSession`, `PassTest`, `SubmitWork`).
- **`GroupNode`**: Represents a branch node. Contains `Rule` (`GroupRule` enum: `InOrder`, `Choice`), an optional `ChoiceCount` (int?) defining how many children must be completed if the rule is `Choice`, and a `Children` (List<ProgramNode>) collection.

## 4. API Reference

### Create a Program
**POST** `/programs`
Creates a new nested program from a JSON tree structure and returns the created tree with server-assigned IDs.
*Example Request Body (from `ProgramNodeRequestDto`):*
```json
{
  "key": "root",
  "name": "My Program",
  "nodeType": "Group",
  "rule": "InOrder",
  "children": [
    {
      "key": "step1",
      "name": "First Step",
      "nodeType": "Step",
      "stepType": "AttendSession"
    }
  ]
}
```
*Example Response Body (from `ProgramNodeResponseDto`):*
```json
{
  "id": "50ffcdef-746b-4d78-bb57-7fdfa30ee964",
  "name": "My Program",
  "nodeType": "Group",
  "rule": "InOrder",
  "children": [
    {
      "id": "6e756026-4b56-44e0-83e2-44c2ce266c79",
      "name": "First Step",
      "nodeType": "Step",
      "stepType": "AttendSession"
    }
  ]
}
```

### Get a Program
**GET** `/programs/{id}`
Retrieves the fully nested program tree by its root `id`.
*Example Response Body:* (same shape as the `POST` response `ProgramNodeResponseDto`)

### Validate a Program
**POST** `/programs/{id}/validate`
Validates an existing program tree for impossible prerequisites and reachability warnings, returning a `ValidationResult`.
*Example Response Body:*
```json
{
  "isValid": true,
  "impossiblePrerequisites": [],
  "reachabilityWarnings": [
    {
      "nodeId": "1a4c0370-56dc-4f70-8229-39227f48884f",
      "nodeName": "Dependent Node",
      "prerequisiteId": "262ae1c7-0e01-41de-b557-3eb08f032ca6",
      "prerequisiteName": "Target Node",
      "reason": "Target is inside 'Choice Group', a choice of 1 of 2 — participants who choose a different option will never satisfy this prerequisite"
    }
  ]
}
```

## 5. Running Tests
The project contains an xUnit test suite covering the validation engine: the full Computer Science scenario, direct-cycle rejection, self-reference rejection, a prerequisite pointing inside its own subtree, reachability warnings under nested choice groups, and boundary cases (choice groups where all options are mandatory, and warnings two levels up the ancestor chain). To run the tests:
```bash
dotnet test
```
**Current status:** 9 passing tests, 0 failures.

## 6. Design Decisions and Trade-offs

**SQL Server vs SQLite.** SQLite was the original plan — it needs no separately installed database engine, so a fresh `git clone` runs immediately with zero setup, which fit the "must run from a clean clone" requirement well. I switched to SQL Server LocalDB/SQLEXPRESS, prioritizing development speed given my existing familiarity with it, and the extra setup step is small for anyone with Visual Studio already installed. The trade-off: a reviewer without a local SQL Server instance will need one extra setup step before the project runs, and the connection string's instance name (`DESKTOP-QEDOLN5\SQLEXPRESS01`) is specific to my machine and must be updated to match theirs — both are called out explicitly in the setup section above to minimize friction. Migrations and the TPH configuration are otherwise identical across either provider.

**Flattened storage vs nested JSON.** The database stores the program tree as flat rows in a single `ProgramNodes` table (self-referencing via `ParentId`, using EF Core's Table-per-Hierarchy pattern for the `Step`/`Group` split) rather than as a single nested JSON blob. This keeps the data queryable and relational — e.g. `WHERE ProgramId = X` retrieves an entire tree in one round trip — and matches how EF Core and SQL Server are meant to be used, rather than working against them. The API's request/response shape, however, is the natural nested JSON tree from the spec, since that's the natural way to author and read a program structure. A `TreeBuilder` converts between the two representations: `Flatten` walks the incoming JSON and produces flat rows for storage, `Rebuild` walks the flat rows back into nested JSON for responses.

One consequence of this split: when a client submits a new program, none of its nodes have real database IDs yet, so a prerequisite can't reference another node by ID at submission time. I solved this with client-assigned temporary `key` strings per node in the request — a prerequisite refers to another node's `key` (`prerequisiteKey`) rather than a real ID. `Flatten` resolves these in two passes: first assigning every node a real GUID and recording a `key → Guid` map, then resolving each `prerequisiteKey` against that map into a real `PrerequisiteId`. The alternative — requiring the client to pre-generate its own GUIDs — was rejected because it pushes ID-collision responsibility onto the caller for no real benefit here.

A related bug surfaced during development and is worth noting: the root node's `Id` and the program's `ProgramId` were initially generated separately, which meant `POST /programs` returned an `id` that didn't match what was stored in the `ProgramId` column, so a follow-up `GET` for that same id returned 404. Fixed by making the root node's own `Id` serve as the `ProgramId` for every node in that tree, since the program *is* its root group — no separate `Program` identity is needed. This is reflected in the "Data model" note above.

## 7. AI Tool Usage
This project was built with **Antigravity**, used phase-by-phase rather than as a single "build everything" prompt, with each phase's output reviewed and manually tested before moving to the next:

1. **Scaffolding** — solution structure, the `ProgramNode`/`StepNode`/`GroupNode` domain classes, and the EF Core TPH configuration for SQL Server.
2. **`POST /programs` and `GET /programs/{id}`** — the request/response DTOs, the `TreeBuilder` (flatten on write, rebuild on read), and the controller endpoints. Manually tested via the API docs UI with the full Computer Science scenario from the spec, and caught/fixed a root-`Id`-vs-`ProgramId` bug this way (see trade-offs above).
3. **`ValidationService` and `POST /programs/{id}/validate`** — the impossible-prerequisite checks (self-reference, inside-own-subtree, appears-later) and the reachability-warning logic (walking every ancestor of a prerequisite's target to detect risky `Choice` groups). The algorithm's design — what to check and in what order — was worked out by hand first and then handed to Antigravity to implement, specifically because this is the part of the challenge most candidates are expected to get wrong, and I wanted to be able to explain and defend the logic myself rather than just the generated code.
4. **Tests** — xUnit tests for the required scenarios plus additional boundary cases (mandatory "choice" groups, multi-level-deep reachability). One real bug was caught this way: the "appears later" check was running before the "inside its own subtree" check, misreporting the reason for one impossible-prerequisite case; fixed by reordering the checks. A second failing test (expecting 2 flagged nodes for a mutual A↔B cycle instead of 1) turned out to be a wrong test expectation rather than a code bug, given how document-order validity is defined here, so the test's expected value was corrected instead of the code.
5. **README** — generated from the actual project files for technical accuracy (routes, property names, exact commands), with this trade-offs section and this AI-usage section written by hand.
