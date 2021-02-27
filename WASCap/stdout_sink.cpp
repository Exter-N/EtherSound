#include "stdafx.h"

#include "stdout_sink.h"

wascap::sink::stdout_sink::stdout_sink(std::unique_ptr<sink> next)
	: chain_sink(std::move(next))
{
}

bool wascap::sink::stdout_sink::can_play() const
{
	return true;
}

bool wascap::sink::stdout_sink::is_playing() const
{
	return true;
}

bool wascap::sink::stdout_sink::process(const float* samples, size_t frames)
{
	fwrite(samples, sizeof(float), frames * channels(), stdout);

	return chain_sink::process(samples, frames);
}

void wascap::sink::stdout_sink::flush()
{
	fflush(stdout);

	chain_sink::flush();
}