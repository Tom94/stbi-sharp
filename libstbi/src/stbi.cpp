// This file was developed by Thomas Müller <thomas94@gmx.net>.
// It is published under the BSD 3-Clause License within the LICENSE file.

#include <inttypes.h>
#include <string.h>
#include <cmath>

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

bool is_linear_qoi(const unsigned char* data, int64_t len) {
    qoi_desc desc;
    if (!qoi_decode_header(data, (int)len, &desc)) {
        return false;
    }
    return desc.colorspace == QOI_LINEAR;
}

bool g_should_flip_vertically = false;
bool g_used_qoi = false;

template <typename T>
void flip_vertically(T* dst, const T* src, int width, int height, int n_channels) {
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

float srgb_to_linear(float srgb) {
    if (srgb <= 0.04045f) {
        return srgb / 12.92f;
    } else {
        return std::pow((srgb + 0.055f) / 1.055f, 2.4f);
    }
}

void srgb_int_to_linear_float(float* dst, const unsigned char* src, int width, int height, int n_channels) {
    int n_non_alpha = (n_channels == 1 || n_channels == 3) ? n_channels : (n_channels - 1);
    for (int i = 0; i < width * height; ++i) {
        for (int c = 0; c < n_non_alpha; ++c) {
            dst[i * n_channels + c] = srgb_to_linear(src[i * n_channels + c] / 255.0f);
        }
    }
    if (n_non_alpha < n_channels) {
        for (int i = 0; i < width * height; ++i) {
            dst[i * n_channels + n_channels - 1] = src[i * n_channels + n_channels - 1] / 255.0f;
        }
    }
}

void linear_int_to_linear_float(float* dst, const unsigned char* src, int width, int height, int n_channels) {
    for (int i = 0; i < width * height * n_channels; ++i) {
        dst[i] = src[i] / 255.0f;
    }
}

bool is_hdr_from_memory(const unsigned char* data, int64_t len) {
    if (is_qoi(data, len)) {
        return false;
    } else {
        return stbi_is_hdr_from_memory(data, (int)len);
    }
}

template <typename T>
T* load_from_memory(const unsigned char* data, int64_t len, int32_t* w, int32_t* h, int32_t* n_channels, int n_desired_channels);

template <>
unsigned char* load_from_memory(const unsigned char* data, int64_t len, int32_t* w, int32_t* h, int32_t* n_channels, int n_desired_channels) {
    unsigned char* pixels;

    if (is_qoi(data, len)) {
        qoi_desc desc;
        pixels = (unsigned char*)qoi_decode(data, (int)len, &desc, n_desired_channels);
        *w = desc.width;
        *h = desc.height;
        *n_channels = desc.channels;
    } else {
        int iw = 0, ih = 0, ic = 0;
        pixels = stbi_load_from_memory(data, (int)len, &iw, &ih, &ic, n_desired_channels);
        *w = iw;
        *h = ih;
        *n_channels = ic;
    }

    if (!pixels) {
        return nullptr;
    }

    if (g_should_flip_vertically) {
        int n_returned_channels = (n_desired_channels == 0) ? *n_channels : n_desired_channels;
        unsigned char* dst = (unsigned char*)malloc(((size_t)(*w) * (*h)) * n_returned_channels);
        flip_vertically(dst, pixels, *w, *h, n_returned_channels);
        free(pixels);
        pixels = dst;
    }

    return pixels;
}

template <>
float* load_from_memory(const unsigned char* data, int64_t len, int32_t* w, int32_t* h, int32_t* n_channels, int n_desired_channels) {
    float* pixels;

    if (is_hdr_from_memory(data, len)) {
        int iw = 0, ih = 0, ic = 0;
        pixels = stbi_loadf_from_memory(data, (int)len, &iw, &ih, &ic, n_desired_channels);
        *w = iw;
        *h = ih;
        *n_channels = ic;
        if (!pixels) {
            return nullptr;
        }

        if (g_should_flip_vertically) {
            int n_returned_channels = (n_desired_channels == 0) ? *n_channels : n_desired_channels;
            float* dst = (float*)malloc(sizeof(float) * (*w) * (*h) * n_returned_channels);
            flip_vertically(dst, pixels, *w, *h, n_returned_channels);
            free(pixels);
            pixels = dst;
        }
    } else {
        // If the image is LDR, promote it to HDR using the sRGB transform.
        unsigned char* ldr = load_from_memory<unsigned char>(data, len, w, h, n_channels, n_desired_channels);
        if (!ldr) {
            return nullptr;
        }

        int n_returned_channels = (n_desired_channels == 0) ? *n_channels : n_desired_channels;
        pixels = (float*)malloc(sizeof(float) * (*w) * (*h) * n_returned_channels);

        if (is_linear_qoi(data, len)) {
            linear_int_to_linear_float(pixels, ldr, *w, *h, n_returned_channels);
        } else {
            srgb_int_to_linear_float(pixels, ldr, *w, *h, n_returned_channels);
        }

        free(ldr);
    }

    return pixels;
}

template <typename T>
bool load_from_memory_info_buffer(const unsigned char* data, int64_t len, int n_desired_channels, T* dst) {
    // Dummy variables that are not going to be used. Returning them is unnecessary, because the provided destination buffer
    // needs to already have the correct size, hence the caller must have already requested the width, height, and number of
    // channels beforehand.
    int32_t width, height, n_channels;

    // Don't flip the image yet, even if desired, because we can then avoid one additional memcpy below
    // by directly flipping the buffer from `tmp` into `dst`.
    bool needs_flipping = g_should_flip_vertically;
    g_should_flip_vertically = false;
    T* tmp = load_from_memory<T>(data, len, &width, &height, &n_channels, n_desired_channels);
    g_should_flip_vertically = needs_flipping;

    if (!tmp) {
        return false;
    }

    if (n_desired_channels != 0) {
        n_channels = n_desired_channels;
    }

    if (needs_flipping) {
        flip_vertically(dst, tmp, width, height, n_channels);
    } else {
        memcpy(dst, tmp, ((size_t)width * height) * n_channels * sizeof(T));
    }
    return true;
}

bool info_from_memory(const unsigned char* data, int64_t len, int32_t* w, int32_t* h, int32_t* n_channels) {
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
        int iw = 0, ih = 0, ic = 0;
        bool success = stbi_info_from_memory(data, (int)len, &iw, &ih, &ic) != 0;
        *w = iw;
        *h = ih;
        *n_channels = ic;
        return success;
    }
}

extern "C" {
    EXPORT int32_t LoadFromMemoryIntoBuffer(const unsigned char* data, int64_t len, int32_t n_desired_channels, unsigned char* dst) {
        return load_from_memory_info_buffer<unsigned char>(data, len, n_desired_channels, dst);
    }

    EXPORT int32_t LoadFFromMemoryIntoBuffer(const unsigned char* data, int64_t len, int32_t n_desired_channels, float* dst) {
        return load_from_memory_info_buffer<float>(data, len, n_desired_channels, dst);
    }

    EXPORT int32_t InfoFromMemory(const unsigned char* data, int64_t len, int32_t* w, int32_t* h, int32_t* n_channels) {
        return info_from_memory(data, len, w, h, n_channels);
    }

    EXPORT int32_t IsHdrFromMemory(const unsigned char* data, int64_t len) {
        return is_hdr_from_memory(data, len);
    }

    EXPORT unsigned char* LoadFromMemory(const unsigned char* data, int64_t len, int32_t* w, int32_t* h, int32_t* n_channels, int32_t n_desired_channels) {
        return load_from_memory<unsigned char>(data, len, w, h, n_channels, n_desired_channels);
    }

    EXPORT float* LoadFFromMemory(const unsigned char* data, int64_t len, int32_t* w, int32_t* h, int32_t* n_channels, int32_t n_desired_channels) {
        return load_from_memory<float>(data, len, w, h, n_channels, n_desired_channels);
    }

    EXPORT void SetFlipVerticallyOnLoad(int32_t should_flip) {
        g_should_flip_vertically = should_flip;
    }

    EXPORT void Free(void* pixels) {
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
