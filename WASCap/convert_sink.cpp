#include "stdafx.h"

#include <intrin.h>
#include <algorithm>
#include <vector>

#include "convert_sink.h"

using wascap::sink::MAX_CHANNELS;

namespace
{
	int gcd(int a, int b)
	{
		return (b == 0) ? a : gcd(b, a % b);
	}

	size_t fill_sink_buffer(float* dest, size_t destcap, size_t& destlen, const float*& src, size_t& srclen, size_t channels)
	{
		size_t n = min(destcap - destlen, srclen);
		if (n > 0) {
			memcpy(dest, src, n * channels * sizeof(float));
			destlen += n;
			srclen -= n;
			src += n * channels;
		}

		return n;
	}

#define NONE MAX_CHANNELS
	unsigned long fallback_channels[MAX_CHANNELS * 3] = {
		1, 0, NONE, NONE, 5, 4, 7, 6,
		NONE, 10, 9, NONE, 14, NONE, 12, 17,
		NONE, 15, NONE, NONE, NONE, NONE, NONE, NONE,
		NONE, NONE, NONE, NONE, NONE, NONE, NONE, NONE,

		2, 2, 0, NONE, 8, 8, 0, 1,
		4, NONE, NONE, NONE, 13, 12, 13, 16,
		15, 16, NONE, NONE, NONE, NONE, NONE, NONE,
		NONE, NONE, NONE, NONE, NONE, NONE, NONE, NONE,

		NONE, NONE, 1, NONE, NONE, NONE, 2, 2,
		5, NONE, NONE, NONE, NONE, 14, NONE, NONE,
		17, NONE, NONE, NONE, NONE, NONE, NONE, NONE,
		NONE, NONE, NONE, NONE, NONE, NONE, NONE, NONE,
	};
#undef NONE
}

size_t wascap::util::unpack_channel_mask(unsigned long* channels, size_t max_channels, DWORD channel_mask)
{
	unsigned long index;

	size_t n_channels = 0;
	while (n_channels < max_channels && _BitScanForward(&index, channel_mask)) {
		channels[n_channels++] = index;
		channel_mask &= channel_mask - 1;
	}

	return n_channels;
}

wascap::sink::samplerate_convert_sink::samplerate_convert_sink(std::unique_ptr<sink> next, size_t samplerate)
	: chain_sink(std::move(next), samplerate), m_target_samplerate(this->next().samplerate()), m_source_samplerate(samplerate), m_mappings(nullptr), m_buffer(nullptr), m_frames_in_buffer(0)
{
	size_t divisor = gcd(m_target_samplerate, m_source_samplerate);
	m_source_samplerate /= divisor;
	m_target_samplerate /= divisor;
	m_mappings = std::make_unique<lerp[]>(m_target_samplerate);
	for (size_t i = 0; i < m_target_samplerate; ++i) {
		float j = (i == 0) ? 0.0f : (i * (m_source_samplerate - 1) / (float)(m_target_samplerate - 1));
		lerp& mapping(m_mappings[i]);
		mapping.first_index = (size_t)j;
		mapping.second_index = mapping.first_index + 1;
		mapping.second_ratio = j - mapping.first_index;
		mapping.first_ratio = 1.0f - mapping.second_ratio;
		if (0.0f == mapping.second_ratio) {
			mapping.second_index = mapping.first_index;
		}
	}
	m_buffer = std::make_unique<float[]>(m_source_samplerate * channels());
}

void wascap::sink::samplerate_convert_sink::convert(float*& destination, const float*& source)
{
	size_t ch = channels();

	for (size_t i = 0; i < m_target_samplerate; ++i) {
		lerp& mapping(m_mappings[i]);
		for (size_t c = 0; c < ch; ++c) {
			destination[(i * ch) + c] = source[(mapping.first_index * ch) + c] * mapping.first_ratio + source[(mapping.second_index * ch) + c] * mapping.second_ratio;
		}
	}

	destination += m_target_samplerate * ch;
	source += m_source_samplerate * ch;
}

bool wascap::sink::samplerate_convert_sink::process(const float* samples, size_t frames)
{
	size_t ch = channels();

	if (m_frames_in_buffer > 0) {
		fill_sink_buffer(m_buffer.get(), m_source_samplerate, m_frames_in_buffer, samples, frames, ch);
	}

	size_t full_buffers = (m_frames_in_buffer + frames) / m_source_samplerate;
	if (0 == full_buffers) {
		return false;
	}

	size_t n_buffers = full_buffers;

	std::unique_ptr<float[]> converted = std::make_unique<float[]>(full_buffers * m_target_samplerate * ch);
	float* cur_converted = converted.get();
	if (m_frames_in_buffer) {
		m_frames_in_buffer = 0;
		const float* buffer = m_buffer.get();
		convert(cur_converted, buffer);
		--n_buffers;
	}

	while (n_buffers > 0) {
		convert(cur_converted, samples);
		frames -= m_source_samplerate;
		--n_buffers;
	}

	fill_sink_buffer(m_buffer.get(), m_source_samplerate, m_frames_in_buffer, samples, frames, ch);

	return chain_sink::process(converted.get(), full_buffers * m_target_samplerate);
}

void wascap::sink::samplerate_convert_sink::flush()
{
	if (m_frames_in_buffer) {
		size_t ch = channels();

		for (size_t i = m_frames_in_buffer; i < m_source_samplerate; ++i) {
			for (size_t c = 0; c < ch; ++c) {
				m_buffer[(i * ch) + c] = 0.0f;
			}
		}

		std::unique_ptr<float[]> converted = std::make_unique<float[]>(m_target_samplerate * ch);
		{
			float* cur_converted = converted.get();
			const float* buffer = m_buffer.get();
			convert(cur_converted, buffer);
		}

		next().process(converted.get(), (m_frames_in_buffer * m_target_samplerate + m_source_samplerate - 1) / m_source_samplerate);
		m_frames_in_buffer = 0;
	}

	chain_sink::flush();
}

wascap::sink::channel_convert_sink::channel_convert_sink(std::unique_ptr<sink> next, DWORD channel_mask)
	: chain_sink(std::move(next), channel_mask)
{
	unsigned long target_channels[MAX_CHANNELS];
	size_t n_target_channels = util::unpack_channel_mask(target_channels, this->next().channels(), this->next().channel_mask());

	unsigned long source_channels[MAX_CHANNELS];
	size_t n_source_channels = util::unpack_channel_mask(source_channels, __popcnt(channel_mask), channel_mask);

	for (size_t i = 0; i < n_target_channels; ++i) {
		size_t raw_mapping = std::find(source_channels, source_channels + n_source_channels, target_channels[i]) - source_channels;
		if (raw_mapping == n_source_channels && MAX_CHANNELS != fallback_channels[target_channels[i]]) {
			raw_mapping = std::find(source_channels, source_channels + n_source_channels, fallback_channels[target_channels[i]]) - source_channels;
		}
		if (raw_mapping != n_source_channels) {
			m_mappings[i * 2] = raw_mapping;
			m_mappings[(i * 2) + 1] = SIZE_MAX;
			continue;
		}
		if (MAX_CHANNELS != fallback_channels[MAX_CHANNELS + target_channels[i]]) {
			raw_mapping = std::find(source_channels, source_channels + n_source_channels, fallback_channels[MAX_CHANNELS + target_channels[i]]) - source_channels;
		}
		size_t raw_mapping2 = n_source_channels;
		if (MAX_CHANNELS != fallback_channels[(MAX_CHANNELS * 2) + target_channels[i]]) {
			raw_mapping2 = std::find(source_channels, source_channels + n_source_channels, fallback_channels[(MAX_CHANNELS * 2) + target_channels[i]]) - source_channels;
		}
		m_mappings[i * 2] = (raw_mapping == n_source_channels) ? SIZE_MAX : raw_mapping;
		m_mappings[(i * 2) + 1] = (raw_mapping2 == n_source_channels) ? SIZE_MAX : raw_mapping2;
	}
	for (size_t i = n_target_channels; i < MAX_CHANNELS; ++i) {
		m_mappings[i * 2] = SIZE_MAX;
		m_mappings[(i * 2) + 1] = SIZE_MAX;
	}
}

bool wascap::sink::channel_convert_sink::process(const float* samples, size_t frames)
{
	size_t target_channels = next().channels();
	std::vector<float> resamples;
	resamples.reserve(frames * target_channels);
	for (size_t i = 0; i < frames; ++i) {
		for (size_t c = 0; c < target_channels; ++c) {
			size_t source_c1 = m_mappings[c * 2];
			size_t source_c2 = m_mappings[(c * 2) + 1];
			resamples.push_back((SIZE_MAX == source_c1) ?
				((SIZE_MAX == source_c2) ? 0.0f : samples[(i * channels()) + source_c2]) :
				((SIZE_MAX == source_c2) ? samples[(i * channels()) + source_c1] : 0.5f * (samples[(i * channels()) + source_c1] + samples[(i * channels()) + source_c2])));
		}
	}

	return chain_sink::process(resamples.data(), frames);
}