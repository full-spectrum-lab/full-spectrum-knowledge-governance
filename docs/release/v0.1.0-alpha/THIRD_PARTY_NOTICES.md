# Third-party notices

The production projects declare no external NuGet package dependencies. They use the .NET runtime and the operating-system SQLite library. Their licenses and notices remain governed by their respective distributors.

Project license expression: `MulanPSL-2.0 OR Apache-2.0`.

This alpha contains synthetic fixtures only and no bundled real regulatory, enterprise or personal knowledge.

`SBOM.spdx.json` is the minimal source-level declaration. The packaging script additionally emits `SBOM.package.spdx.json`, with `filesAnalyzed=true`, a SHA-256 for every packaged payload file, and package-to-file relationships. The framework-dependent .NET runtime and the operating-system SQLite library are runtime prerequisites rather than bundled payloads.
