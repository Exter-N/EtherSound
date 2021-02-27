#pragma once

#include <windows.h>
#include <intrin.h>
#include <memory>

#include "no_copy.h"

namespace wascap
{
	namespace sink
	{
		constexpr size_t MAX_CHANNELS = sizeof(DWORD) << 3;

		class sink : util::no_copy_no_move
		{
			size_t m_samplerate;
			DWORD m_channel_mask;

		protected:
			inline sink(size_t samplerate, DWORD channel_mask) : m_samplerate(samplerate), m_channel_mask(channel_mask) { }

		public:
			virtual ~sink();

			inline size_t samplerate() const { return m_samplerate; }
			inline size_t channels() const { return __popcnt(m_channel_mask); }
			inline DWORD channel_mask() const { return m_channel_mask; }

			virtual bool can_play() const = 0;

			virtual bool is_open() const = 0;
			virtual bool is_playing() const = 0;

			virtual bool process(const float* samples, size_t frames) = 0;
			virtual void flush() = 0;
		};

		class null_sink : public sink
		{
		public:
			null_sink(size_t samplerate, DWORD channel_mask);

			virtual bool can_play() const;

			virtual bool is_open() const;
			virtual bool is_playing() const;

			virtual bool process(const float* samples, size_t frames);
			virtual void flush();
		};

		class chain_sink : public sink
		{
			std::unique_ptr<sink> m_next;

		protected:
			chain_sink(std::unique_ptr<sink> next);
			chain_sink(std::unique_ptr<sink> next, size_t samplerate);
			chain_sink(std::unique_ptr<sink> next, DWORD channel_mask);
			chain_sink(std::unique_ptr<sink> next, size_t samplerate, DWORD channel_mask);

			inline sink& next() const { return *m_next; }

		public:
			virtual bool can_play() const;

			virtual bool is_open() const;
			virtual bool is_playing() const;

			virtual bool process(const float* samples, size_t frames);
			virtual void flush();
		};
	}
}