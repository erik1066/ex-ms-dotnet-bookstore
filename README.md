# ASP.NET Core 2.2 Example Bookstore Microservice
This is a repository containing an example ASP.NET Core 2.2 microservice with basic REST APIs for a fictional bookstore. It is intended for C# / ASP.NET Core 2.x developers to fork and modify. It includes implementations for:

* Dependency injection
* Logging
* [FDNS .NET Core SDK](https://github.com/erik1066/fdns-dotnet-sdk)
* Circuit breakers
* OAuth2 scope-based authorization
* [FDNS Object microservice](https://github.com/CDCGov/fdns-ms-object) for all database CRUD operations
* [FDNS Storage microservice](https://github.com/CDCGov/fdns-ms-storage) for storing CSV files used for bulk record imports
* Auto-generated, live API documentation via Swagger pages and C# XML code comments
* Cross-origin resource sharing
* Exponential backoff for HTTP requests
* Easy containerization via a `Dockerfile` and `Makefile`
* Two-stage Docker builds
* Health checks

# Foundation Services (FDNS) Overview
This example bookstore microservice uses [the FDNS .NET Core SDK](https://github.com/erik1066/ex-ms-dotnet-bookstore) to interact with the [Foundation Services](https://github.com/CDCGov/fdns). The Foundation Services are a collection of general purpose, open source microservices authored by the United States Centers for Disease Control and Prevention (CDC). They can be assembled in various arrangements to rapidly and efficiently build modern, open, and interoperable software systems. FDNS are considered "foundational" because of their high reusability and broad applicability to wide array of use cases. 

In hands-on terms, software developers can use FDNS as the underlying infrastructure for new IT system design to save time and money - while giving their organization's analysts easier access to data. For instance, FDNS includes services for CRUD operations, indexing and search, raw file storage, cryptography, generic business rule validation, health messaging validation and parsing, and document parsing and conversions, among others. These services expose simple HTTP REST APIs that developers can easily interact with using plain HTTP or via an FDNS SDK that wraps those HTTP calls.

The FDNS microservices are highly-scalable and very efficient. An organization can stand up the FDNS microservices and enable hundreds of IT systems to call the FDNS APIs, using the power of the underlying container engine to horizontally scale the microservices to rapidly meet organizational demand. Centralizing core IT capabilities behind configurable FDNS endpoints means less duplication of effort across the enterprise: For example, instead of 20 product teams each determining how to implement indexing and search in isolation from one another (perhaps with expensive analysis-of-alternatives and prototyping, etc.), and then designing and implementing 20 separate implementations, those product teams can instead reuse the FDNS search/indexing services with simple HTTP API calls.

To see how FDNS can be used as a basis for new IT system design, please see [CDC's FDNS repo on GitHub](https://github.com/CDCGov/fdns).

FDNS are portable to any container infrastructure, including Docker Swarm, Kubernetes, and OpenShift. FDNS Docker images can be found at [the U.S. CDC's DockerHub page](https://hub.docker.com/u/cdcgov).

## Modifying this microservice
See [USAGE.md](/docs/USAGE.md) for instructions on how to debug and containerize this microservice in your local environment.

See [the docs folder](/docs/README.md) for an example of how to modify this microservice.

## License
The repository utilizes code licensed under the terms of the Apache Software License and therefore is licensed under ASL v2 or later.

This source code in this repository is free: you can redistribute it and/or modify it under
the terms of the Apache Software License version 2, or (at your option) any later version.

This source code in this repository is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
PARTICULAR PURPOSE. See the Apache Software License for more details.

You should have received a copy of the Apache Software License along with this program. If not, see https://www.apache.org/licenses/LICENSE-2.0.html.

The source code forked from other open source projects will inherit its license.