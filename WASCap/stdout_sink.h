#pragma once

#include <memory>

#include "base_sink.h"

namespace wascap
{
	namespace sink
	{
		class stdout_sink : public chain_sink
		{
		public:
			stdout_sink(std::unique_ptr<sink> next);

			virtual bool can_play() const;

			virtual bool is_playing() const;

			virtual bool process(const float* samples, size_t frames);
			virtual void flush();
		};
	}
}