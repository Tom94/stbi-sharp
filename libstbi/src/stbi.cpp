#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

#define EXPORT __attribute__((visibility("default")))

extern "C" {
    EXPORT bool stbiInfo(const unsigned char* data, int64_t len, int* w, int* h, int* nChannels) {
        return stbi_info_from_memory(data, len, w, h, nChannels) == 1;
    }

    EXPORT bool stbiLoad(unsigned char* dst, const unsigned char* data, int64_t len, int nDesiredChannels) {
        int width, height, nChannels;
        unsigned char* tmp = stbi_load_from_memory(data, len, &width, &height, &nChannels, nDesiredChannels);

        if (!tmp) {
            return false;
        }

        size_t nBytes = ((size_t)width * height) * nDesiredChannels;
        for (int i = 0; i < nBytes; ++i) {
            dst[i] = tmp[i];
        }

        stbi_image_free(tmp);
        return true;
    }

    EXPORT unsigned char* stbiLoadRaw(const unsigned char* data, int64_t len, int* w, int* h, int* nChannels, int nDesiredChannels) {
        return stbi_load_from_memory(data, len, w, h, nChannels, nDesiredChannels);
    }

    EXPORT void stbiFreeRaw(unsigned char* pixels) {
        stbi_image_free(pixels);
    }
}
