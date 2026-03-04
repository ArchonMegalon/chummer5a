<p align="center"><h1>Chummer 5</h1></p>
<p align="center"><img src="https://i.ibb.co/y0WC3j9/logo.png"></p>

[![Github Latest Release Date](https://img.shields.io/github/release-date/chummer5a/chummer5a?label=Latest%20Milestone%20Release)](https://github.com/chummer5a/chummer5a/releases/latest)
[![GitHub Issues](https://img.shields.io/github/issues/chummer5a/chummer5a.svg)](https://github.com/chummer5a/chummer5a/issues)
[![Build status](https://ci.appveyor.com/api/projects/status/wf0jbqd5xp05s4hs?svg=true)](https://ci.appveyor.com/project/chummer5a/chummer5a)
[![Discord](https://img.shields.io/discord/365227581018079232?label=discord)](https://discord.gg/8FKUPjTX2w)
[![License](https://img.shields.io/github/license/chummer5a/chummer5a)](https://www.gnu.org/licenses/gpl-3.0.html)
[![Donations](https://img.shields.io/badge/buy%20me%20a%20coffee-donate-yellow.svg)](https://ko-fi.com/Z8Z7IP4E)

## Project Overview

Chummer is a character creation and management application for the tabletop RPG [Shadowrun, Fifth Edition](https://www.shadowruntabletop.com/products-page/getting-started/shadowrun-fifth-edition).

This repository currently has two active tracks:

* **Legacy path**: the WinForms desktop app (`Chummer`) that continues to serve as compatibility reference and regression oracle.
* **Modern migration path (Docker branch)**: API + shared presentation seam + two UI heads (`Chummer.Blazor`, `Chummer.Avalonia`).

## Docker Branch Status

The `Docker` branch is an active migration branch and no longer follows a WinForms-only architecture:

* `Chummer.Api` is the HTTP host for headless services and workspace routes.
* `Chummer.Application`, `Chummer.Contracts`, `Chummer.Infrastructure`, and `Chummer.Presentation` provide the shared behavior seam.
* `Chummer.Blazor` and `Chummer.Avalonia` are the two UI heads over the same presentation/API path.
* `Chummer.Web` is currently retained as a temporary legacy-shell parity artifact during migration.
* Runtime compose flows target `chummer-api` and `chummer-blazor`; no `chummer-web` service is part of the active product path.
* Migration execution backlog: [`docs/MIGRATION_BACKLOG.md`](docs/MIGRATION_BACKLOG.md).

`docker-compose.yml` exposes:

* `chummer-api` (default service)
* `chummer-blazor` (default service)
* `chummer-blazor-portal` (under the `portal` profile; internal `/blazor` path-base host)
* `chummer-portal` (under the `portal` profile; single landing + proxy gateway)
* `chummer-tests` (under the `test` profile only)

## Running the Docker Branch

The `Docker` branch is validated on Linux with `net10.0` tests through Docker and uses .NET 10 containers.

Start API only:

```bash
docker compose up -d --build chummer-api
```

Start API + Blazor UI:

```bash
docker compose up -d --build chummer-api chummer-blazor
```

Start API + Blazor + Portal landing surface:

```bash
docker compose --profile portal up -d --build chummer-api chummer-blazor-portal chummer-portal
```

Enable API key protection (recommended for production):

```bash
export CHUMMER_API_KEY="replace-with-strong-secret"
docker compose up -d --build chummer-api chummer-blazor
```

When set, `Chummer.Api` enforces `X-Api-Key` for non-public `/api/*` routes and both UI heads automatically forward the key.

Run migration/compliance test loop (branch helper script):

```bash
bash scripts/migration-loop.sh 1
```

Run Linux test profile directly:

```bash
docker compose --profile test run --rm chummer-tests
```

Default endpoints:

* API root: `http://127.0.0.1:8088/`
* API health: `http://127.0.0.1:8088/api/health`
* Blazor UI: `http://127.0.0.1:8089/`
* Blazor health: `http://127.0.0.1:8089/health`
* Portal landing (profile `portal`): `http://127.0.0.1:8091/`

Portal notes (current milestone):

* `/api` and `/docs` are served via in-process portal proxy routing.
* `/blazor` is served through an in-process portal proxy to an internal `chummer-blazor-portal` instance configured with `CHUMMER_BLAZOR_PATH_BASE=/blazor`.
* `/downloads` remains redirect-based.
* Non-portal default flows keep `chummer-blazor` at root and do not require path-base configuration.

## Legacy WinForms Requirements
| Operating System | .NET Framework |
| --- | --- |
| Windows 7 SP1 or 8.1+ | 4.8+ |

## Installation - Windows

Chummer uses a single tree release strategy with two release channels; **Milestone** and **Nightly**.

* **Milestone** releases are a fixed-point for use by living communities and people that prefer not to update their application regularly. These releases are considered to be stable and are recommended for general use. 
* **Nightly** releases are an automated build created with Appveyor at 0000 UTC daily. These releases are more likely to be unstable, but also receive new features and bugfixes faster than the Milestone releases. These are recommended for users that have a specific issue from Milestone that was resolved in Nightly, or are comfortable with testing features. 

1. Download the archive for your preferred update channel [Milestone](https://github.com/chummer5a/chummer5a/releases/latest) or [Nightly](https://github.com/chummer5a/chummer5a/releases) (Select the latest Nightly tag)
2. Extract to preferred folder location. If upgrading, you can extract over the top of an existing folder path.
3. Run Chummer5.exe.

## Installation - Linux and OSX

For the legacy WinForms desktop app, support for other operating systems is limited. For Linux, macOS, and Chrome OS, legacy Chummer can be run through one of three possible ways:

1. Set up and run [Wine](https://www.winehq.org/), an open-source Windows compatibility layer. This is usually not for the faint-of-heart, especially on Chrome OS, but it is completely free. Some details about the steps necessary to run Chummer5a under Wine can be found on [the wiki](https://github.com/chummer5a/chummer5a/wiki#installation). Note that even after you set up Chummer5a to run on Wine, Wine is not perfect and you will encounter some additional bugs while using Chummer5a that you wouldn't run into under Windows.
2. Set up and run [CrossOver](https://www.codeweavers.com/crossover), a hassle-free version of Wine with commercial support. It costs money (though it has a limited free trial), but what you are effectively purchasing is for someone else to do all the hard work setting up Wine for you, no matter what you want to run on it. If you do not want to mess around with technical stuff, we highly recommend using CrossOver.
3. Set up and run a Windows virtual machine through programs like [VirtualBox](https://www.virtualbox.org/), [VMWare Fusion](https://www.vmware.com/products/fusion.html), or [Parallels](https://www.parallels.com/). You will need a valid copy of Windows and lots of disk space, but Chummer5a will run on a Windows virtual machine exactly how it would run under full Windows. Virtual machine hosts are generally not available for Chrome OS, though with some behind-the-scenes tinkering, it can still be possible to run a Windows virtual machine on Chrome OS.

## Contributing

Please take a look at our [contributing](https://github.com/chummer5a/chummer5a/blob/master/CONTRIBUTING.md) guidelines if you're interested in helping!

## History

This project is a continuation of work on the original Chummer projects for Shadowrun 4th and 5th editions, developed by Keith Rudolph and Adam Schmidt. Due to the closure of code.google.com, github repositories of their code have been created as a marker of their work. Please note, Chummer 4 is considered abandonware and is not maintained by the chummer5a team, and exists solely for historical purposes.

* Chummer 4, Keith Rudolph: https://github.com/chummer5a/chummer
* Chummer 5, Keith Rudolph and Adam Schmidt: https://github.com/chummer5a/chummer5

## Sponsors

* [JetBrains](http://www.jetbrains.com/) have been kind enough to provide our development team with licences for their excellent tools:
    * [ReSharper](http://www.jetbrains.com/resharper/)
