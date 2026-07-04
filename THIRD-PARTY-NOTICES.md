# Third-Party Notices

This project (the modern async wrapper under `src/`) is licensed under the MIT License
(see [LICENSE](LICENSE)). It builds on top of the following third-party components, each
under its own license. The MIT license of this project does **not** relicense any of them.

## Interactive Brokers TWS API - NOT open source

- **Component:** Interactive Brokers TWS API (C# client), included as a git submodule at
  `vendor/tws-api` (upstream: https://github.com/InteractiveBrokers/tws-api).
- **Copyright:** © Interactive Brokers LLC. All rights reserved.
- **License:** *IB API Non-Commercial License* or *IB API Commercial License*, as applicable
  (see the copyright header in each IBKR source file).

**Important:** This wrapper does not redistribute the IBKR API - it is fetched from
Interactive Brokers as a submodule. Your use of the IBKR API is governed by Interactive
Brokers' license terms, **not** by this project's MIT license. In particular, the default
IB API license is **non-commercial**; obtaining commercial rights to the IBKR API is a matter
between you and Interactive Brokers. Nothing in this repository grants you any rights to the
IBKR API.

## Google.Protobuf

- **Used by:** the IBKR C# client (`CSharpAPI.csproj` references `Google.Protobuf` 3.29.5).
- **License:** BSD-3-Clause. © Google Inc. https://github.com/protocolbuffers/protobuf

## Microsoft.Extensions.* (Options, DependencyInjection.Abstractions)

- **Used by:** the wrapper's dependency-injection integration (`AddTwsApi`, `ITwsClientFactory`).
- **License:** MIT. © .NET Foundation and Contributors. https://github.com/dotnet/runtime

## Intel® Decimal Floating-Point Math Library

- **Note:** Interactive Brokers' native/reference distribution notes use of the Intel® Decimal
  Floating-Point Math Library under Intel's license. It is referenced here for completeness; it
  is not part of the managed C# build path used by this wrapper.
