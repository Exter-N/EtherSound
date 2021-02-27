#include "stdafx.h"

#include "was_source.h"
#include "errors.h"

wascap::source::was_source::was_source(const was::mm_device& device)
{
	m_audio_client = device.activate<IAudioClient>(CLSCTX_ALL, nullptr);

	{
		WAVEFORMATEX* pwfx;
		COM_CHECK(m_audio_client->GetMixFormat(&pwfx));
		m_wave_format.reset(pwfx);
	}

	{
		const WAVEFORMATEX& format = wave_format();

		if (format.wFormatTag != WAVE_FORMAT_IEEE_FLOAT && (format.wFormatTag != WAVE_FORMAT_EXTENSIBLE && ((const WAVEFORMATEXTENSIBLE&)format).SubFormat != KSDATAFORMAT_SUBTYPE_IEEE_FLOAT)) {
			throw std::runtime_error("Invalid WAS source format");
		}
	}

	COM_CHECK(m_audio_client->Initialize(AUDCLNT_SHAREMODE_SHARED,
		(device.data_flow() == eRender) ? AUDCLNT_STREAMFLAGS_LOOPBACK : 0,
		10000000, 0, m_wave_format.get(), nullptr));
}

void wascap::source::was_source::run(sink::sink& sink, size_t stop_after_frames)
{
	{
		const WAVEFORMATEX& format = wave_format();

		if (sink.samplerate() != format.nSamplesPerSec || sink.channel_mask() != was::channel_mask(format)) {
			throw std::runtime_error("Incompatible sink");
		}
	}

	util::com_ptr<IAudioCaptureClient> capture_client;
	COM_CHECK(m_audio_client->GetService(__uuidof(IAudioCaptureClient), capture_client.ppv()));

	COM_CHECK(m_audio_client->Start());
	try {
		UINT32 packetLength;
		BYTE* pData;
		UINT32 numFramesAvailable;
		DWORD flags;

		bool shall_flush = false;
		while (sink.is_open() && sink.is_playing() && stop_after_frames > 0) {
			COM_CHECK(capture_client->GetNextPacketSize(&packetLength));
			if (0 == packetLength) {
				Sleep(6);
				continue;
			}
			COM_CHECK(capture_client->GetBuffer(&pData, &numFramesAvailable, &flags, nullptr, nullptr));
			if (sink.process((const float*)pData, numFramesAvailable)) {
				shall_flush = true;
			}
			COM_CHECK(capture_client->ReleaseBuffer(numFramesAvailable));
			stop_after_frames = (stop_after_frames > numFramesAvailable) ? (stop_after_frames - numFramesAvailable) : 0;
		}
		if (shall_flush) {
			sink.flush();
		}
	}
	catch (...) {
		m_audio_client->Stop();
		throw;
	}

	COM_CHECK(m_audio_client->Stop());
}