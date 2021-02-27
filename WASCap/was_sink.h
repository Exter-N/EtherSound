#pragma once

#include <audioclient.h>
#include <memory>

#include "com_helper.h"
#include "base_sink.h"
#include "mm_device.h"

namespace wascap
{
	namespace sink
	{
		class was_sink : public chain_sink
		{
			util::com_ptr<IAudioClient> m_audio_client;
			util::com_ptr<IAudioRenderClient> m_render_client;
			WAVEFORMATEXTENSIBLE m_wave_format;
			UINT32 m_buffer_frame_count;

		public:
			was_sink(std::unique_ptr<sink> next, const was::mm_device& device);

			virtual bool can_play() const;

			virtual bool is_playing() const;

			virtual bool process(const float* samples, size_t frames);
		};
	}
}