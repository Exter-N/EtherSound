#include "stdafx.h"

#include <windows.h>
#include <vector>

#include "convert_sink.h"
#include "shmctl_sink.h"
#include "errors.h"
#include "string_format.h"

namespace shmctl = wascap::shmctl;

namespace
{
	float update_weighted_average(float& average, float sample, double alpha)
	{
		return average = (float)(alpha * average + (1 - alpha) * sample);
	}

	void update_max_amplitude(float& max_amplitude, float sample)
	{
		if (sample > max_amplitude) {
			max_amplitude = sample;
		}
		else if (sample < -max_amplitude) {
			max_amplitude = -sample;
		}
	}

	void circular_write(char* buffer, size_t capacity, volatile int& cursor, const char* data, size_t length)
	{
		size_t cursor_snapshot = cursor;
		for (;;) {
			size_t segment_length = min(length, capacity - cursor_snapshot);
			if (!segment_length) {
				break;
			}
			memcpy(buffer + cursor_snapshot, data, segment_length);
			cursor_snapshot = (cursor_snapshot + segment_length) % capacity;
			data += segment_length;
			length -= segment_length;
		}
		cursor = cursor_snapshot;
	}
}

wascap::shmctl::shmctl::shmctl(const std::string& shm_name)
	: m_hshm(nullptr), m_shmblock(nullptr)
{
#if UNICODE
	{
		std::unique_ptr<wchar_t[]> shm_name_w = util::wstr_from_string(shm_name);
		m_hshm = WIN32_CHECK(OpenFileMappingW(FILE_MAP_WRITE, false, shm_name_w.get()));
	}
#else
	m_hshm = WIN32_CHECK(OpenFileMappingA(FILE_MAP_WRITE, false, shm_name.c_str()));
#endif

	try {
		m_shmblock = (volatile shm_contents*)WIN32_CHECK(MapViewOfFile(m_hshm, FILE_MAP_ALL_ACCESS, 0, 0, 0));
	}
	catch (...) {
		CloseHandle(m_hshm);
		throw;
	}
}

wascap::shmctl::shmctl::~shmctl()
{
	UnmapViewOfFile((LPCVOID)m_shmblock);
	CloseHandle(m_hshm);
}

bool wascap::shmctl::shmctl::is_open() const
{
	return (m_shmblock->flags & (SHMCTL_FLAG_INITIALIZED | SHMCTL_FLAG_ABORT_REQUESTED)) != (SHMCTL_FLAG_INITIALIZED | SHMCTL_FLAG_ABORT_REQUESTED);
}

bool wascap::shmctl::shmctl::is_playing() const
{
	return (m_shmblock->flags & (SHMCTL_FLAG_INITIALIZED | SHMCTL_FLAG_ENABLED)) == (SHMCTL_FLAG_INITIALIZED | SHMCTL_FLAG_ENABLED) && m_shmblock->master_volume != 0.0f;
}

wascap::sink::shmctl_sink::shmctl_sink(std::unique_ptr<sink> next, const std::shared_ptr<shmctl::shmctl>& shmctl)
	: chain_sink(std::move(next)), m_shmctl(shmctl)
{
}

wascap::sink::shmctl_flow_control_sink::shmctl_flow_control_sink(std::unique_ptr<sink> next, const std::shared_ptr<shmctl::shmctl>& shmctl)
	: shmctl_sink(std::move(next), shmctl)
{
}

bool wascap::sink::shmctl_flow_control_sink::is_open() const
{
	return chain_sink::is_open() && shmctl().is_open();
}

bool wascap::sink::shmctl_flow_control_sink::is_playing() const
{
	return chain_sink::is_playing() && shmctl().is_playing();
}

bool wascap::sink::shmctl_flow_control_sink::process(const float* samples, size_t frames)
{
	if (!shmctl().is_open() || !shmctl().is_playing()) {
		return false;
	}

	return chain_sink::process(samples, frames);
}

wascap::sink::shmctl_averaging_sink::shmctl_averaging_sink(std::unique_ptr<sink> next, const std::shared_ptr<shmctl::shmctl>& shmctl)
	: shmctl_sink(std::move(next), shmctl), m_last { 0.0f }
{
}

bool wascap::sink::shmctl_averaging_sink::process(const float* samples, size_t frames)
{
	if (frames > 0) {
		volatile shmctl::shm_contents* shmblock = shmctl().get();

		double averaging_weight = shmblock->averaging_weight;
		if (0.0 != averaging_weight) {
			std::vector<float> resamples;
			resamples.reserve(frames * channels());
			for (size_t i = 0; i < frames; ++i) {
				for (size_t c = 0; c < channels(); ++c) {
					resamples.push_back(update_weighted_average(m_last[c], samples[(i * channels()) + c], averaging_weight));
				}
			}

			return chain_sink::process(resamples.data(), frames);
		}
	}

	return chain_sink::process(samples, frames);
}

void wascap::sink::shmctl_averaging_sink::flush()
{
	for (size_t c = 0; c < MAX_CHANNELS; ++c) {
		m_last[c] = 0.0f;
	}

	chain_sink::flush();
}

wascap::sink::shmctl_volume_sink::shmctl_volume_sink(std::unique_ptr<sink> next, const std::shared_ptr<shmctl::shmctl>& shmctl)
	: shmctl_sink(std::move(next), shmctl)
{
	for (size_t c = util::unpack_channel_mask(m_channel_mappings, channels(), channel_mask()); c < MAX_CHANNELS; ++c) {
		m_channel_mappings[c] = MAX_CHANNELS;
	}

	volatile shmctl::shm_contents* shmblock = shmctl->get();

	shmblock->samplerate = samplerate();
	shmblock->channel_mask = channel_mask();
}

bool wascap::sink::shmctl_volume_sink::process(const float* samples, size_t frames)
{
	volatile shmctl::shm_contents* shmblock = shmctl().get();

	size_t ch = channels();
	float max_amplitudes[MAX_CHANNELS] = { 0.0f };

	for (size_t i = 0; i < frames; ++i) {
		for (size_t c = 0; c < ch; ++c) {
			update_max_amplitude(max_amplitudes[c], samples[(i * ch) + c]);
		}
	}

	float channel_volumes[MAX_CHANNELS];
	for (size_t c = 0; c < ch; ++c) {
		channel_volumes[c] = shmblock->channel_volumes[m_channel_mappings[c]];
	}

	float max_amplitude = 0.0f;
	for (size_t c = 0; c < ch; ++c) {
		max_amplitude = max(max_amplitude, max_amplitudes[c] * channel_volumes[c]);
	}

	float silence_threshold = shmblock->silence_threshold;
	float saturation_threshold = shmblock->saturation_threshold;
	float saturation_debounce_factor = shmblock->saturation_debounce_factor;

	float saturation_effective_volume = shmblock->saturation_effective_volume;
	float saturation_debounce_volume = shmblock->saturation_debounce_volume;

	if (saturation_threshold != 0.0f) {
		if (max_amplitude * saturation_effective_volume > saturation_threshold) {
			saturation_effective_volume = saturation_threshold / max_amplitude;
		}
		if (max_amplitude * saturation_debounce_volume / saturation_debounce_factor > saturation_threshold) {
			saturation_debounce_volume = (saturation_threshold / max_amplitude) * saturation_debounce_factor;
		}
	}
	saturation_debounce_volume *= shmblock->saturation_recovery_factor;
	if (saturation_debounce_volume > 1.0f) {
		saturation_debounce_volume = 1.0f;
	}
	if (saturation_effective_volume < saturation_debounce_volume) {
		saturation_effective_volume = saturation_debounce_volume;
	}

	shmblock->last_frame_tick_count = GetTickCount64();
	shmblock->last_frame_max_amplitude = max_amplitude;
	shmblock->saturation_debounce_volume = saturation_debounce_volume;
	shmblock->saturation_effective_volume = saturation_effective_volume;

	if (max_amplitude < silence_threshold) {
		return false;
	}

	float final_master_volume = shmctl().is_playing()
		? shmblock->master_volume * saturation_effective_volume
		: 0.0f;

	if (max_amplitude * final_master_volume < silence_threshold) {
		return false;
	}

	float final_channel_volumes[MAX_CHANNELS];
	bool has_volume_adjustment = false;
	for (size_t c = 0; c < ch; ++c) {
		final_channel_volumes[c] = final_master_volume * channel_volumes[c];
		has_volume_adjustment |= 1.0f != final_channel_volumes[c];
	}

	if (has_volume_adjustment) {
		std::vector<float> resamples;
		resamples.reserve(frames * ch);
		for (size_t i = 0; i < frames; ++i) {
			for (size_t c = 0; c < ch; ++c) {
				resamples.push_back(samples[(i * ch) + c] * final_channel_volumes[c]);
			}
		}

		return chain_sink::process(resamples.data(), frames);
	}

	return chain_sink::process(samples, frames);
}

wascap::sink::shmctl_tap_sink::shmctl_tap_sink(std::unique_ptr<sink> next, const std::shared_ptr<shmctl::shmctl>& shmctl)
	: shmctl_sink(std::move(next), shmctl)
{
}

bool wascap::sink::shmctl_tap_sink::can_play() const
{
	if (chain_sink::can_play()) {
		return true;
	}

	volatile shmctl::shm_contents* shmblock = shmctl().get();

	return shmblock->tap_capacity > 0;
}

bool wascap::sink::shmctl_tap_sink::is_playing() const
{
	if (chain_sink::is_playing()) {
		return true;
	}

	volatile shmctl::shm_contents* shmblock = shmctl().get();

	return shmblock->tap_capacity > 0;
}

bool wascap::sink::shmctl_tap_sink::process(const float* samples, size_t frames)
{
	volatile shmctl::shm_contents* shmblock = shmctl().get();

	size_t tap_capacity = shmblock->tap_capacity;
	if (tap_capacity > 0) {
		char* tap_buffer = ((char*)shmblock) + shmblock->tap_offset;
		circular_write(tap_buffer, tap_capacity, shmblock->tap_write_cursor, (const char*)samples, frames * sizeof(float) * channels());
	}

	return chain_sink::process(samples, frames);
}