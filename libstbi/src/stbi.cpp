#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

#ifdef _WIN32
    #define EXPORT __declspec(dllexport)
#elif __APPLE__
    #define EXPORT __attribute__((visibility("default")))
#else
    #define EXPORT
#endif

extern "C" {
    EXPORT bool StbiLoadFromMemoryIntoBuffer(const unsigned char* data, int64_t len, int nDesiredChannels, unsigned char* dst) {
        // Dummy variables that are not going to be used. Returning them is unnecessary, because the probided destination buffer
        // needs to already have the correct size, hence the caller must have already requested the width, height, and number of
        // channels beforehand.
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

    EXPORT bool StbiInfoFromMemory(const unsigned char* data, int64_t len, int* w, int* h, int* nChannels) {
        return stbi_info_from_memory(data, len, w, h, nChannels) == 1;
    }

    EXPORT unsigned char* StbiLoadFromMemory(const unsigned char* data, int64_t len, int* w, int* h, int* nChannels, int nDesiredChannels) {
        return stbi_load_from_memory(data, len, w, h, nChannels, nDesiredChannels);
    }

    EXPORT void StbiFree(unsigned char* pixels) {
        stbi_image_free(pixels);
    }
}
