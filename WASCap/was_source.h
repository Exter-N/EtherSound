#pragma once

#include <audioclient.h>

#include "com_helper.h"
#include "no_copy.h"
#include "mm_device.h"
#include "base_sink.h"

namespace wascap
{
	namespace source
	{
		class was_source : public util::no_copy_no_move
		{
			util::com_ptr<IAudioClient> m_audio_client;
			util::co_task_unique_ptr<WAVEFORMATEX> m_wave_format;

		public:
			explicit was_source(const was::mm_device& device);

			inline const WAVEFORMATEX& wave_format() const { return *m_wave_format; }

			void run(sink::sink& sink, size_t stop_after_frames);
		};
	}
}