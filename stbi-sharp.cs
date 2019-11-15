// This file was developed by Thomas Müller <thomas94@gmx.net>.
// It is published under the BSD 3-Clause License within the LICENSE file.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StbiSharp
{
    unsafe public class StbiImage : IDisposable
    {
        private byte* data;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int NumChannels { get; private set; }

        public ReadOnlySpan<byte> Data => new ReadOnlySpan<byte>(data, Width * Height * NumChannels);

        public StbiImage(byte* data, int width, int height, int numChannels)
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

        ~StbiImage()
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

    public class Stbi
    {
        [DllImport("stbi")]
        unsafe public static extern bool LoadFromMemoryIntoBuffer(byte* data, long len, byte* dst);

        unsafe public static void LoadFromMemoryIntoBuffer(byte[] data, byte[] dst)
        {
            fixed (byte* address = data)
                fixed (byte* dstAddress = dst)
                    if (!LoadFromMemoryIntoBuffer(address, data.Length, dstAddress))
                        throw new ArgumentException($"STBI could not load an image from the provided {nameof(data)}.");
        }

        public static void LoadFromMemoryInfoBuffer(MemoryStream m, byte[] dst) =>
            LoadFromMemoryIntoBuffer(m.GetBuffer(), dst);

        [DllImport("stbi")]
        unsafe public static extern bool InfoFromMemory(byte* data, long len, out int width, out int height, out int numChannels);

        unsafe public static bool InfoFromMemory(byte[] data, out int width, out int height, out int numChannels)
        {
            fixed (byte* address = data)
                return InfoFromMemory(address, data.Length, out width, out height, out numChannels);
        }

        public static bool InfoFromMemory(MemoryStream m, out int width, out int height, out int numChannels)
            => InfoFromMemory(m.GetBuffer(), out width, out height, out numChannels);

        [DllImport("stbi")]
        unsafe public static extern byte* LoadFromMemory(byte* data, long len, out int width, out int height, out int numChannels, int desiredNumChannels);

        [DllImport("stbi")]
        unsafe public static extern void Free(byte* pixels);

        unsafe public static StbiImage LoadFromMemory(byte[] data, int desiredNumChannels)
        {
            fixed (byte* address = data) {
                byte* pixels = LoadFromMemory(address, data.Length, out int width, out int height, out int numChannels, desiredNumChannels);
                if (pixels == null) {
                    throw new ArgumentException($"STBI could not load an image from the provided {nameof(data)}.");
                }

                return new StbiImage(pixels, width, height, desiredNumChannels == 0 ? numChannels : desiredNumChannels);
            }
        }

        public static StbiImage LoadFromMemory(MemoryStream m, int desiredNumChannels)
            => LoadFromMemory(m.GetBuffer(), desiredNumChannels);
    }
}
