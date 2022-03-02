// This file was developed by Thomas Müller <thomas94@gmx.net>.
// It is published under the BSD 3-Clause License within the LICENSE file.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StbiSharp
{
    unsafe public class StbiImageBase : IDisposable
    {
        protected void* data = null;

        /// <summary>
        /// The width of the image in number of pixels.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// The height of the image in number of pixels.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// The number of colour channels of the image.
        /// </summary>
        public int NumChannels { get; private set; }

        internal StbiImageBase(void* data, int width, int height, int numChannels)
        {
            this.data = data;

            Width = width;
            Height = height;
            NumChannels = numChannels;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (data != null)
            {
                Stbi.Free(data);
                data = null;
            }
        }

        ~StbiImageBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// A disposable class that exposes image data and metadata for images loaded via STBI.
    /// On disposal, frees any native memory that has been allocated to store the image data.
    /// </summary>
    unsafe public class StbiImage : StbiImageBase
    {
        /// <summary>
        /// The raw image data. It is stored in in row-major order, pixel by pixel. Each pixel consists
        /// of <see cref="NumChannels"/> bytes ordered RGBA.
        /// </summary>
        public ReadOnlySpan<byte> Data => new ReadOnlySpan<byte>(data, Width * Height * NumChannels);

        internal StbiImage(byte* data, int width, int height, int numChannels)
            : base(data, width, height, numChannels)
        {
        }
    }

    /// <summary>
    /// A disposable class that exposes image data and metadata for images loaded via STBI.
    /// On disposal, frees any native memory that has been allocated to store the image data.
    /// </summary>
    unsafe public class StbiImageF : StbiImageBase
    {
        /// <summary>
        /// The raw image data. It is stored in in row-major order, pixel by pixel. Each pixel consists
        /// of <see cref="NumChannels"/> floats ordered RGBA.
        /// </summary>
        public ReadOnlySpan<float> Data => new ReadOnlySpan<float>(data, Width * Height * NumChannels);

        internal StbiImageF(float* data, int width, int height, int numChannels)
            : base(data, width, height, numChannels)
        {
        }
    }

    public class Stbi
    {
        /// <summary>
        /// Loads an encoded image (in PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/> into <paramref name="dst"/>. Requires an additional copy
        /// to <paramref name="dst"/> and is thus slower than <see cref="LoadFromMemory"/>.
        /// </summary>
        /// <param name="data">Pointer to the beginning of the encoded image data.</param>
        /// <param name="len">Number of bytes that the encoded image data is long.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <param name="dst">Pointer to the beginning of the destination buffer into which the
        /// image is loaded. The loaded image will be stored in this buffer in row-major format, pixel
        /// by pixel. Each pixel consists of N bytes where N is the number of channels, ordered RGBA.</param>
        /// <returns>True on success, false on failure.</returns>
        [DllImport("stbi")]
        unsafe public static extern bool LoadFromMemoryIntoBuffer(byte* data, long len, int desiredNumChannels, byte* dst);

        /// <summary>
        /// Loads an encoded image (in PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/> into <paramref name="dst"/>. Requires an additional copy
        /// to <paramref name="dst"/> and is thus slower than <see cref="LoadFromMemory"/>.
        /// </summary>
        /// <param name="data">The encoded image data to be loaded.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <param name="dst">The destination buffer into which the image is loaded. The loaded image
        /// will be stored in this buffer in row-major format, pixel by pixel. Each pixel consists of
        /// N bytes where N is the number of channels, ordered RGBA.</param>
        /// <exception cref="ArgumentException">Thrown when image loading fails.</exception>
        unsafe public static void LoadFromMemoryIntoBuffer(ReadOnlySpan<byte> data, int desiredNumChannels, Span<byte> dst)
        {
            fixed (byte* address = data)
            fixed (byte* dstAddress = dst)
                if (!LoadFromMemoryIntoBuffer(address, data.Length, desiredNumChannels, dstAddress))
                    throw new ArgumentException($"STBI could not load an image from the provided {nameof(data)}: {FailureReason()}");
        }

        /// <summary>
        /// Loads an encoded image (in PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/> into <paramref name="dst"/>. Requires an additional copy
        /// to <paramref name="dst"/> and is thus slower than <see cref="LoadFromMemory"/>.
        /// </summary>
        /// <param name="data">The encoded image data to be loaded.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <param name="dst">The destination buffer into which the image is loaded. The loaded image
        /// will be stored in this buffer in row-major format, pixel by pixel. Each pixel consists of
        /// N bytes where N is the number of channels, ordered RGBA.</param>
        /// <exception cref="ArgumentException">Thrown when image loading fails.</exception>
        public static void LoadFromMemoryInfoBuffer(MemoryStream data, int desiredNumChannels, Span<byte> dst) =>
            LoadFromMemoryIntoBuffer(data.GetBuffer(), desiredNumChannels, dst);

        /// <summary>
        /// Loads an encoded image (in HDR, PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/> into <paramref name="dst"/>. Requires an additional copy
        /// to <paramref name="dst"/> and is thus slower than <see cref="LoadFFromMemory"/>.
        /// The image is returned in floating point format, which preserves the full dynamic range of HDR files.
        /// When loading non-HDR images, they will be converted to floating point.
        /// sRGB images will be remapped to linear, assuming a gamma of 2.2.
        /// </summary>
        /// <param name="data">Pointer to the beginning of the encoded image data.</param>
        /// <param name="len">Number of bytes that the encoded image data is long.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <param name="dst">Pointer to the beginning of the destination buffer into which the
        /// image is loaded. The loaded image will be stored in this buffer in row-major format, pixel
        /// by pixel. Each pixel consists of N floats where N is the number of channels, ordered RGBA.</param>
        /// <returns>True on success, false on failure.</returns>
        [DllImport("stbi")]
        unsafe public static extern bool LoadFFromMemoryIntoBuffer(byte* data, long len, int desiredNumChannels, float* dst);

        /// <summary>
        /// Loads an encoded image (in HDR, PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/> into <paramref name="dst"/>. Requires an additional copy
        /// to <paramref name="dst"/> and is thus slower than <see cref="LoadFFromMemory"/>.
        /// The image is returned in floating point format, which preserves the full dynamic range of HDR files.
        /// When loading non-HDR images, they will be converted to floating point.
        /// sRGB images will be remapped to linear, assuming a gamma of 2.2.
        /// </summary>
        /// <param name="data">The encoded image data to be loaded.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <param name="dst">The destination buffer into which the image is loaded. The loaded image
        /// will be stored in this buffer in row-major format, pixel by pixel. Each pixel consists of
        /// N floats where N is the number of channels, ordered RGBA.</param>
        /// <exception cref="ArgumentException">Thrown when image loading fails.</exception>
        unsafe public static void LoadFFromMemoryIntoBuffer(ReadOnlySpan<byte> data, int desiredNumChannels, Span<float> dst)
        {
            fixed (byte* address = data)
            fixed (float* dstAddress = dst)
                if (!LoadFFromMemoryIntoBuffer(address, data.Length, desiredNumChannels, dstAddress))
                    throw new ArgumentException($"STBI could not load an image from the provided {nameof(data)}: {FailureReason()}");
        }

        /// <summary>
        /// Loads an encoded image (in HDR, PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/> into <paramref name="dst"/>. Requires an additional copy
        /// to <paramref name="dst"/> and is thus slower than <see cref="LoadFFromMemory"/>.
        /// The image is returned in floating point format, which preserves the full dynamic range of HDR files.
        /// When loading non-HDR images, they will be converted to floating point.
        /// sRGB images will be remapped to linear, assuming a gamma of 2.2.
        /// </summary>
        /// <param name="data">The encoded image data to be loaded.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <param name="dst">The destination buffer into which the image is loaded. The loaded image
        /// will be stored in this buffer in row-major format, pixel by pixel. Each pixel consists of
        /// N floats where N is the number of channels, ordered RGBA.</param>
        /// <exception cref="ArgumentException">Thrown when image loading fails.</exception>
        public static void LoadFFromMemoryInfoBuffer(MemoryStream data, int desiredNumChannels, Span<float> dst) =>
            LoadFFromMemoryIntoBuffer(data.GetBuffer(), desiredNumChannels, dst);

        /// <summary>
        /// Retrieves metadata from an encoded image (in PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Pointer to the beginning of the encoded image data.</param>
        /// <param name="len">Number of bytes that the encoded image data is long.</param>
        /// <param name="width">The number of pixels the image is wide.</param>
        /// <param name="height">The number of pixels the image is tall.</param>
        /// <param name="numChannels">The number of colour channels of the image.</param>
        /// <returns>True on success, false on failure.</returns>
        [DllImport("stbi")]
        unsafe public static extern bool InfoFromMemory(byte* data, long len, out int width, out int height, out int numChannels);

        /// <summary>
        /// Retrieves metadata from an encoded image (in PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The encoded image data.</param>
        /// <param name="width">The number of pixels the image is wide.</param>
        /// <param name="height">The number of pixels the image is tall.</param>
        /// <param name="numChannels">The number of colour channels of the image.</param>
        /// <exception cref="ArgumentException">Thrown when image metadata loading fails.</exception>
        unsafe public static void InfoFromMemory(ReadOnlySpan<byte> data, out int width, out int height, out int numChannels)
        {
            fixed (byte* address = data)
                if (!InfoFromMemory(address, data.Length, out width, out height, out numChannels))
                    throw new ArgumentException($"STBI could not load image metadata from the provided {nameof(data)}: {FailureReason()}");
        }

        /// <summary>
        /// Retrieves metadata from an encoded image (in PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The encoded image data.</param>
        /// <param name="width">The number of pixels the image is wide.</param>
        /// <param name="height">The number of pixels the image is tall.</param>
        /// <param name="numChannels">The number of colour channels of the image.</param>
        /// <exception cref="ArgumentException">Thrown when image metadata loading fails.</exception>
        public static void InfoFromMemory(MemoryStream data, out int width, out int height, out int numChannels)
            => InfoFromMemory(data.GetBuffer(), out width, out height, out numChannels);

        /// <summary>
        /// Returns whether or not an encoded image is an HDR image (e.g. Radiance .HDR image; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported HDR formats)
        /// </summary>
        /// <param name="data">Pointer to the beginning of the encoded image data.</param>
        /// <param name="len">Number of bytes that the encoded image data is long.</param>
        [DllImport("stbi")]
        unsafe public static extern bool IsHdrFromMemory(byte* data, long len);

        /// <summary>
        /// Returns whether or not an encoded image is an HDR image (e.g. Radiance .HDR image; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported HDR formats)
        /// </summary>
        /// <param name="data">The encoded image data.</param>
        unsafe public static bool IsHdrFromMemory(ReadOnlySpan<byte> data)
        {
            fixed (byte* address = data)
                return IsHdrFromMemory(address, data.Length);
        }

        /// <summary>
        /// Returns whether or not an encoded image is an HDR image (e.g. Radiance .HDR image; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported HDR formats)
        /// </summary>
        /// <param name="data">The encoded image data.</param>
        public static bool IsHdrFromMemory(MemoryStream data)
            => IsHdrFromMemory(data.GetBuffer());

        /// <summary>
        /// Loads an encoded image (in PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Pointer to the beginning of the encoded image data.</param>
        /// <param name="len">Number of bytes that the encoded image data is long.</param>
        /// <param name="width">The number of pixels the image is wide.</param>
        /// <param name="height">The number of pixels the image is tall.</param>
        /// <param name="numChannels">The number of colour channels of the image.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <returns>Null on failure. On success, returns a pointer to the beginning of the buffer into which the
        /// image was loaded. The loaded image will be stored in this buffer in row-major format, pixel
        /// by pixel. Each pixel consists of N bytes where N is the number of channels, ordered RGBA.</returns>
        [DllImport("stbi")]
        unsafe public static extern byte* LoadFromMemory(byte* data, long len, out int width, out int height, out int numChannels, int desiredNumChannels);

        /// <summary>
        /// Loads an encoded image (in HDR, PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/>.
        /// The image is returned in floating point format, which preserves the full dynamic range of HDR files.
        /// When loading non-HDR images, they will be converted to floating point.
        /// sRGB images will be remapped to linear, assuming a gamma of 2.2.
        /// </summary>
        /// <param name="data">Pointer to the beginning of the encoded image data.</param>
        /// <param name="len">Number of bytes that the encoded image data is long.</param>
        /// <param name="width">The number of pixels the image is wide.</param>
        /// <param name="height">The number of pixels the image is tall.</param>
        /// <param name="numChannels">The number of colour channels of the image.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <returns>Null on failure. On success, returns a pointer to the beginning of the buffer into which the
        /// image was loaded. The loaded image will be stored in this buffer in row-major format, pixel
        /// by pixel. Each pixel consists of N floats where N is the number of channels, ordered RGBA.</returns>
        [DllImport("stbi")]
        unsafe public static extern float* LoadFFromMemory(byte* data, long len, out int width, out int height, out int numChannels, int desiredNumChannels);

        /// <summary>
        /// Flip the image vertically, so the first pixel in the output array is the bottom left.
        /// </summary>
        /// <param name="shouldFlip">True if should flip vertically on load.</param>
        [DllImport("stbi")]
        unsafe public static extern void SetFlipVerticallyOnLoad(bool shouldFlip);

        /// <summary>
        /// Frees memory of an image that has previously been loaded by <see cref="LoadFromMemory"/> or <see cref="LoadFFromMemory"/>.
        /// Only has to be called when the byte-pointer overload of <see cref="LoadFromMemory"/>
        /// or the float-pointer overload of <see cref="LoadFFromMemory"/> was used.
        /// </summary>
        /// <param name="data">Pointer to the beginning of the pixel data.</param>
        [DllImport("stbi")]
        unsafe public static extern void Free(void* data);

        /// <summary>
        /// After failure to load an image, returns a pointer to a string describing the reason for the failure.
        /// </summary>
        [DllImport("stbi", EntryPoint = "FailureReason")]
        unsafe public static extern IntPtr FailureReasonIntPtr();

        /// <summary>
        /// After failure to load an image, returns a string describing the reason for the failure.
        /// </summary>
        public static string FailureReason() => Marshal.PtrToStringAuto(FailureReasonIntPtr());

        /// <summary>
        /// Loads an encoded image (in PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The encoded image data to be loaded.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <returns>Returns a disposable <see cref="StbiImage"/> object that exposes image data
        /// and metadata. On disposal, <see cref="StbiImage"/> frees any native memory that has
        /// been allocated to store the image data.</returns>
        unsafe public static StbiImage LoadFromMemory(ReadOnlySpan<byte> data, int desiredNumChannels)
        {
            fixed (byte* address = data)
            {
                byte* pixels = LoadFromMemory(address, data.Length, out int width, out int height, out int numChannels, desiredNumChannels);
                if (pixels == null)
                {
                    throw new ArgumentException($"STBI could not load an image from the provided {nameof(data)}: {FailureReason()}");
                }

                return new StbiImage(pixels, width, height, desiredNumChannels == 0 ? numChannels : desiredNumChannels);
            }
        }

        /// <summary>
        /// Loads an encoded image (in PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The encoded image data to be loaded.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <returns>Returns a disposable <see cref="StbiImage"/> object that exposes image data
        /// and metadata. On disposal, <see cref="StbiImage"/> frees any native memory that has
        /// been allocated to store the image data.</returns>
        public static StbiImage LoadFromMemory(MemoryStream data, int desiredNumChannels)
            => LoadFromMemory(data.GetBuffer(), desiredNumChannels);

        /// <summary>
        /// Loads an encoded image (in HDR, PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/>.
        /// The image is returned in floating point format, which preserves the full dynamic range of HDR files.
        /// When loading non-HDR images, they will be converted to floating point.
        /// sRGB images will be remapped to linear, assuming a gamma of 2.2.
        /// </summary>
        /// <param name="data">The encoded image data to be loaded.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <returns>Returns a disposable <see cref="StbiImageF"/> object that exposes image data
        /// and metadata. On disposal, <see cref="StbiImageF"/> frees any native memory that has
        /// been allocated to store the image data.</returns>
        unsafe public static StbiImageF LoadFFromMemory(ReadOnlySpan<byte> data, int desiredNumChannels)
        {
            fixed (byte* address = data)
            {
                float* pixels = LoadFFromMemory(address, data.Length, out int width, out int height, out int numChannels, desiredNumChannels);
                if (pixels == null)
                {
                    throw new ArgumentException($"STBI could not load an image from the provided {nameof(data)}: {FailureReason()}");
                }

                return new StbiImageF(pixels, width, height, desiredNumChannels == 0 ? numChannels : desiredNumChannels);
            }
        }

        /// <summary>
        /// Loads an encoded image (in HDR, PNG, JPG, or another supported format; see the README of
        /// https://github.com/nothings/stb/blob/master/stb_image.h for a list of supported formats)
        /// residing at <paramref name="data"/>.
        /// The image is returned in floating point format, which preserves the full dynamic range of HDR files.
        /// When loading non-HDR images, they will be converted to floating point.
        /// sRGB images will be remapped to linear, assuming a gamma of 2.2.
        /// </summary>
        /// <param name="data">The encoded image data to be loaded.</param>
        /// <param name="desiredNumChannels">The number of desired colour channels in the output.
        /// When the encoded image has fewer channels than the desired number of channels,
        /// then the desired number of channels will be produced automatically. For example,
        /// when the encoded image is RGB, but 4 channels are requested, then a fully opaque
        /// alpha channel will be generated. Supplying a value of 0 means that the native number
        /// of channels of the encoded image is used.</param>
        /// <returns>Returns a disposable <see cref="StbiImageF"/> object that exposes image data
        /// and metadata. On disposal, <see cref="StbiImageF"/> frees any native memory that has
        /// been allocated to store the image data.</returns>
        public static StbiImageF LoadFFromMemory(MemoryStream data, int desiredNumChannels)
            => LoadFFromMemory(data.GetBuffer(), desiredNumChannels);
    }
}
