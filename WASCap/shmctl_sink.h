#pragma once

#include <memory>
#include <string>

#include "base_sink.h"
#include "no_copy.h"

#define SHMCTL_FLAG_INITIALIZED 1
#define SHMCTL_FLAG_ENABLED 2
#define SHMCTL_FLAG_ABORT_REQUESTED 4

namespace wascap
{
	namespace shmctl
	{
#pragma pack(push, 4)
		struct shm_contents
		{
			int flags;
			int tap_offset;
			int tap_write_cursor;
			int tap_capacity;
			float master_volume;
			float channel_volumes[sink::MAX_CHANNELS];
			float saturation_threshold;
			float silence_threshold;
			float averaging_weight;
			float saturation_debounce_factor;
			float saturation_recovery_factor;
			float saturation_debounce_volume;
			float saturation_effective_volume;
			int samplerate;
			DWORD channel_mask;
			ULONGLONG last_frame_tick_count;
			float last_frame_max_amplitude;
		};
#pragma pack(pop)

		class shmctl : public util::no_copy_no_move
		{
			HANDLE m_hshm;
			volatile shm_contents* m_shmblock;

		public:
			shmctl(const std::string& shm_name);
			~shmctl();

			inline volatile shm_contents* get() const { return m_shmblock; }

			bool is_open() const;
			bool is_playing() const;

			inline volatile shm_contents& operator *() const { return *m_shmblock; }
			inline volatile shm_contents* operator ->() const { return m_shmblock; }
		};
	}

	namespace sink
	{
		class shmctl_sink : public chain_sink
		{
			std::shared_ptr<shmctl::shmctl> m_shmctl;

		protected:
			shmctl_sink(std::unique_ptr<sink> next, const std::shared_ptr<shmctl::shmctl>& shmctl);

			inline const shmctl::shmctl& shmctl() const { return *m_shmctl; }
		};

		class shmctl_flow_control_sink : public shmctl_sink
		{
		public:
			shmctl_flow_control_sink(std::unique_ptr<sink> next, const std::shared_ptr<shmctl::shmctl>& shmctl);

			virtual bool is_open() const;
			virtual bool is_playing() const;

			virtual bool process(const float* samples, size_t frames);
		};

		class shmctl_averaging_sink : public shmctl_sink
		{
			float m_last[MAX_CHANNELS];

		public:
			shmctl_averaging_sink(std::unique_ptr<sink> next, const std::shared_ptr<shmctl::shmctl>& shmctl);

			virtual bool process(const float* samples, size_t frames);
			virtual void flush();
		};

		class shmctl_volume_sink : public shmctl_sink
		{
			unsigned long m_channel_mappings[MAX_CHANNELS];

		public:
			shmctl_volume_sink(std::unique_ptr<sink> next, const std::shared_ptr<shmctl::shmctl>& shmctl);

			virtual bool process(const float* samples, size_t frames);
		};

		class shmctl_tap_sink : public shmctl_sink
		{
		public:
			shmctl_tap_sink(std::unique_ptr<sink> next, const std::shared_ptr<shmctl::shmctl>& shmctl);

			virtual bool can_play() const;

			virtual bool is_playing() const;

			virtual bool process(const float* samples, size_t frames);
		};
	}
}
