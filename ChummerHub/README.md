## Legacy ChummerHub

This project is an archived compatibility asset only.

- It is not part of the active solution or public runtime.
- It is not the future ChummerHub product path.
- Active hub, portal, and public-edge work belongs behind `Chummer.Portal` and the shared `Chummer.Api` seams.

If this project is touched at all, changes must stay limited to archival hygiene, compatibility verification, or removal of stale secrets/configuration.

Its Dockerfile fails closed by default because this archived dependency graph still targets unsupported .NET 6 and is not cleared for publication. Isolated migration work must opt in explicitly with `--build-arg CHUMMER_ALLOW_ARCHIVED_HUB_BUILD=1`; that override does not authorize deployment or inclusion in a flagship release. A zero-high/critical dependency audit and supported-runtime migration are required before removing the guard.
