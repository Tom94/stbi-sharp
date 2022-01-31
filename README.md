# StbiSharp

![](https://github.com/tom94/stbi-sharp/workflows/CI/badge.svg)

A C# wrapper around the single-header image-loading libraries [stb_image.h](https://github.com/nothings/stb/blob/master/stb_image.h) and [qoi.h](https://github.com/phoboslab/qoi/blob/master/qoi.h). It supports __JPEG__, __PNG__, __QOI__, __BMP__, __GIF__, HDR, PIC, PNM, PSD, and TGA (for some formats only a subset of features; consult [stb_image.h](https://github.com/nothings/stb/blob/master/stb_image.h) for details).

## Usage

Grab __StbiSharp__ from [nuget](https://www.nuget.org/packages/StbiSharp/), then load an image as follows:
```csharp
public void doSomethingWithImage()
{
    using (var stream = File.OpenRead("some-image.jpg"))
    using (var memoryStream = new MemoryStream())
    {
        stream.CopyTo(memoryStream);
        StbiImage image = Stbi.LoadFromMemory(memoryStream, 4);

        // Use image.Width, image.Height,
        // image.NumChannels, and image.Data.
    }
}
```
If the encoded image is directly available in memory, no file stream needs to be used.


## Building

You are only required to build __StbiSharp__ if you plan to develop it further or fix bugs. In case this applies to you, thank you for helping out! :)

Rather than explaining in text how to build __StbiSharp__, I invite you to check [our GitHub build-and-publish workflow](https://github.com/Tom94/stbi-sharp/blob/master/.github/workflows/main.yml). It contains the minimal set of instructions to build the native STBI lib via a C++ compiler on Ubuntu, macOS, and Windows, as well as the minimal set of instructions for building the C# wrapper using `dotnet`.


## License

__StbiSharp__ is available under the BSD 3-clause license, which you can find in the `LICENSE.txt` file. [TL;DR](https://tldrlegal.com/license/bsd-3-clause-license-(revised)) you can do almost whatever you want as long as you include the original copyright and license notice in any copy of the software and the source code.
