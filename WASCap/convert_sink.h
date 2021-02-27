#pragma once

#include <memory>

#include "base_sink.h"

namespace wascap
{
	namespace sink
	{
		class samplerate_convert_sink : public chain_sink
		{
			struct lerp
			{
				size_t first_index;
				size_t second_index;
				float first_ratio;
				float second_ratio;
			};

			size_t m_target_samplerate;
			size_t m_source_samplerate;
			std::unique_ptr<lerp[]> m_mappings;
			std::unique_ptr<float[]> m_buffer;
			size_t m_frames_in_buffer;

			void convert(float*& destination, const float*& source);

		public:
			samplerate_convert_sink(std::unique_ptr<sink> next, size_t samplerate);

			virtual bool process(const float* samples, size_t frames);
			virtual void flush();
		};

		class channel_convert_sink : public chain_sink
		{
			size_t m_mappings[MAX_CHANNELS * 2];

		public:
			channel_convert_sink(std::unique_ptr<sink> next, DWORD channel_mask);

			virtual bool process(const float* samples, size_t frames);
		};
	}
	namespace util
	{
		size_t unpack_channel_mask(unsigned long* channels, size_t max_channels, DWORD channel_mask);
	}
}