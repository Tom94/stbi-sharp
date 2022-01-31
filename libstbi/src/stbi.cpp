// This file was developed by Thomas Müller <thomas94@gmx.net>.
// It is published under the BSD 3-Clause License within the LICENSE file.

#include <inttypes.h>
#include <string.h>

#define QOI_NO_STDIO
#define QOI_IMPLEMENTATION
#include <qoi.h>

#define STBI_ASSERT(x)
#define STBI_NO_STDIO
#define STBI_FAILURE_USERMSG
#define STB_IMAGE_IMPLEMENTATION
#include <stb_image.h>

#ifdef _WIN32
    #define EXPORT __declspec(dllexport)
#elif __APPLE__
    #define EXPORT __attribute__((visibility("default")))
#else
    #define EXPORT
#endif

bool is_qoi(const unsigned char* data, int64_t len) {
    return len >= 4 && strncmp((const char*)data, "qoif", 4) == 0;
}

bool qoi_decode_header(const void* data, int size, qoi_desc* desc) {
    const unsigned char* bytes;
    unsigned int header_magic;
    int p = 0;

    if (
        data == NULL || desc == NULL ||
        size < QOI_HEADER_SIZE + (int)sizeof(qoi_padding)
    ) {
        return false;
    }

    bytes = (const unsigned char *)data;

    header_magic = qoi_read_32(bytes, &p);
    desc->width = qoi_read_32(bytes, &p);
    desc->height = qoi_read_32(bytes, &p);
    desc->channels = bytes[p++];
    desc->colorspace = bytes[p++];

    if (
        desc->width == 0 || desc->height == 0 ||
        desc->channels < 3 || desc->channels > 4 ||
        desc->colorspace > 1 ||
        header_magic != QOI_MAGIC ||
        desc->height >= QOI_PIXELS_MAX / desc->width
    ) {
        return false;
    }

    return true;
}

bool g_should_flip_vertically = false;
bool g_used_qoi = false;

void flip_vertically(unsigned char* dst, const unsigned char* src, int width, int height, int n_channels) {
    for (int y = 0; y < height; ++y) {
        int target_y = height - y - 1;
        int stride_y = width * n_channels;
        for (int x = 0; x < width; ++x) {
            for (int c = 0; c < n_channels; ++c) {
                dst[target_y * stride_y + x * n_channels + c] = src[y * stride_y + x * n_channels + c];
            }
        }
    }
}

extern "C" {
    EXPORT bool LoadFromMemoryIntoBuffer(const unsigned char* data, int64_t len, int n_desired_channels, unsigned char* dst) {
        // Dummy variables that are not going to be used. Returning them is unnecessary, because the probided destination buffer
        // needs to already have the correct size, hence the caller must have already requested the width, height, and number of
        // channels beforehand.
        int width, height, n_channels;
        unsigned char* tmp;

        if (is_qoi(data, len)) {
            g_used_qoi = true;
            qoi_desc desc;
            tmp = (unsigned char*)qoi_decode(data, (int)len, &desc, n_desired_channels);
            width = desc.width;
            height = desc.height;
            n_channels = desc.channels;
        } else {
            g_used_qoi = false;
            tmp = stbi_load_from_memory(data, (int)len, &width, &height, &n_channels, n_desired_channels);
        }

        if (!tmp) {
            return false;
        }

        if (g_should_flip_vertically) {
            flip_vertically(dst, tmp, width, height, n_channels);
        } else {
            memcpy(dst, tmp, ((size_t)width * height) * n_channels);
        }

        free(tmp);
        return true;
    }

    EXPORT bool InfoFromMemory(const unsigned char* data, int64_t len, int* w, int* h, int* n_channels) {
        if (is_qoi(data, len)) {
            g_used_qoi = true;
            qoi_desc desc;
            if (!qoi_decode_header(data, (int)len, &desc)) {
                return false;
            }
            *w = desc.width;
            *h = desc.height;
            *n_channels = desc.channels;
            return true;
        } else {
            g_used_qoi = false;
            return stbi_info_from_memory(data, (int)len, w, h, n_channels) == 1;
        }
    }

    EXPORT unsigned char* LoadFromMemory(const unsigned char* data, int64_t len, int* w, int* h, int* n_channels, int n_desired_channels) {
        unsigned char* pixels;

        if (is_qoi(data, len)) {
            qoi_desc desc;
            pixels = (unsigned char*)qoi_decode(data, (int)len, &desc, n_desired_channels);
            *w = desc.width;
            *h = desc.height;
            *n_channels = desc.channels;
        } else {
            pixels = stbi_load_from_memory(data, (int)len, w, h, n_channels, n_desired_channels);
        }

        if (g_should_flip_vertically) {
            unsigned char* dst = (unsigned char*)malloc(((size_t)(*w) * (*h)) * (*n_channels));
            flip_vertically(dst, pixels, *w, *h, *n_channels);
            free(pixels);
            pixels = dst;
        }

        return pixels;
    }

    EXPORT void SetFlipVerticallyOnLoad(int should_flip) {
        g_should_flip_vertically = should_flip;
    }

    EXPORT void Free(unsigned char* pixels) {
        free(pixels);
    }

    EXPORT const char* FailureReason() {
        if (g_used_qoi) {
            return "unknown";
        } else {
            return stbi_failure_reason();
        }
    }
}
