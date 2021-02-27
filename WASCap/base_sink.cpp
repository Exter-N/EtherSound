#include "stdafx.h"

#include "base_sink.h"

wascap::sink::sink::~sink()
{
}

wascap::sink::null_sink::null_sink(size_t samplerate, DWORD channel_mask)
	: sink(samplerate, channel_mask)
{
}

bool wascap::sink::null_sink::can_play() const
{
	return false;
}

bool wascap::sink::null_sink::is_open() const
{
	return true;
}

bool wascap::sink::null_sink::is_playing() const
{
	return false;
}

bool wascap::sink::null_sink::process(const float* samples, size_t frames)
{
	return true;
}

void wascap::sink::null_sink::flush()
{
}

wascap::sink::chain_sink::chain_sink(std::unique_ptr<sink> next)
	: sink(next->samplerate(), next->channel_mask()), m_next(std::move(next))
{
}

wascap::sink::chain_sink::chain_sink(std::unique_ptr<sink> next, size_t samplerate)
	: sink(samplerate, next->channel_mask()), m_next(std::move(next))
{
}

wascap::sink::chain_sink::chain_sink(std::unique_ptr<sink> next, DWORD channel_mask)
	: sink(next->samplerate(), channel_mask), m_next(std::move(next))
{
}

wascap::sink::chain_sink::chain_sink(std::unique_ptr<sink> next, size_t samplerate, DWORD channel_mask)
	: sink(samplerate, channel_mask), m_next(std::move(next))
{
}

bool wascap::sink::chain_sink::can_play() const
{
	return m_next->can_play();
}

bool wascap::sink::chain_sink::is_open() const
{
	return m_next->is_open();
}

bool wascap::sink::chain_sink::is_playing() const
{
	return m_next->is_playing();
}

bool wascap::sink::chain_sink::process(const float* samples, size_t frames)
{
	return m_next->process(samples, frames);
}

void wascap::sink::chain_sink::flush()
{
	m_next->flush();
}