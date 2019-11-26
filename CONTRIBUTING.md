## Contributing

First of all, thank you for considering to contribute to __StbiSharp__!

In order to contribute to this project, simply fork it, implement your contribution in a feature branch, and then create a pull request. Feature additions, bugfixes, and other kinds of contributions are _all_ warmly welcome. I try to review every pull request promptly.

## Building

In order to test your additions, you'll have to build __StbiSharp__.

__StbiSharp__ consists of two components:
1. a C++ shared library, __libstbi__, that wraps stb_image.h and exposes a subset of its API, and
2. a C# project that is used by consumers of __StbiSharp__ to invoke the C++ shared library.

__libstbi__ has to be built natively for each supported operating system. Currently, the supportes operating systems are Windows, macOS, and Linux. Support for mobile platforms is planned and should be simple to add. Pull requests are welcome!

The C# project only has to be built once, as it runs on .net standard and/or .net core, which both are platform independent.

Rather than explaining in writing how exactly to build __libstbi__ and __StbiSharp__, I invite you to check [our GitHub build-and-publish workflow](https://github.com/Tom94/stbi-sharp/blob/master/.github/workflows/main.yml). It contains the minimal set of instructions to build __libstbi__ via a C++ compiler on Ubuntu, macOS, and Windows, as well as the minimal set of instructions for building the C# wrapper using `dotnet`.
