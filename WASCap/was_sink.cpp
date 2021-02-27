#include "stdafx.h"

#include "was_sink.h"
#include "string_format.h"

wascap::sink::was_sink::was_sink(std::unique_ptr<sink> next, const was::mm_device& device)
	: chain_sink(std::move(next))
{
	if (device.data_flow() != eRender) {
		throw std::runtime_error(util::string_format("Cannot create WAS sink from capture device %s (%s)", device.id(), device.friendly_name()));
	}

	m_wave_format.Format.wFormatTag = WAVE_FORMAT_EXTENSIBLE;
	m_wave_format.Format.nChannels = (WORD)channels();
	m_wave_format.Format.nSamplesPerSec = samplerate();
	m_wave_format.Format.nAvgBytesPerSec = samplerate() * channels() * sizeof(float);
	m_wave_format.Format.nBlockAlign = (WORD)(channels() * sizeof(float));
	m_wave_format.Format.wBitsPerSample = 32;
	m_wave_format.Format.cbSize = sizeof(m_wave_format);

	m_wave_format.Samples.wValidBitsPerSample = 32;
	m_wave_format.dwChannelMask = channel_mask();
	m_wave_format.SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;

	m_audio_client = device.activate<IAudioClient>(CLSCTX_ALL, nullptr);

	COM_CHECK(m_audio_client->Initialize(AUDCLNT_SHAREMODE_SHARED, 0, 1000000, 0, &m_wave_format.Format, nullptr));

	COM_CHECK(m_audio_client->GetBufferSize(&m_buffer_frame_count));

	COM_CHECK(m_audio_client->GetService(__uuidof(IAudioRenderClient), m_render_client.ppv()));

	COM_CHECK(m_audio_client->Start());
}

bool wascap::sink::was_sink::can_play() const
{
	return true;
}

bool wascap::sink::was_sink::is_playing() const
{
	return true;
}

bool wascap::sink::was_sink::process(const float* samples, size_t frames)
{
	const float* cur_samples = samples;
	size_t n_frames = frames;
	while (n_frames > 0) {
		size_t available_frames;
		for (;;) {
			UINT32 padding;
			COM_CHECK(m_audio_client->GetCurrentPadding(&padding));
			available_frames = m_buffer_frame_count - padding;
			if (available_frames > 0) {
				break;
			}
			Sleep(6);
		}

		size_t actual_frames = min(available_frames, n_frames);
		size_t actual_samples = actual_frames * channels();

		BYTE* data;
		COM_CHECK(m_render_client->GetBuffer(actual_frames, &data));

		memcpy(data, cur_samples, actual_samples * sizeof(float));
		cur_samples += actual_samples;
		n_frames -= actual_frames;

		COM_CHECK(m_render_client->ReleaseBuffer(actual_frames, 0));
	}

	return chain_sink::process(samples, frames);
}