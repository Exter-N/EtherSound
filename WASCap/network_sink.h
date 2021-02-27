#pragma once

#include <memory>
#include <vector>

#include "base_sink.h"
#include "wsa_helper.h"

namespace wascap
{
	namespace sink
	{
		class network_sink : public chain_sink
		{
			util::shared_wsa m_wsa;
			util::wsa_socket m_socket;
			std::vector<char> m_peername;
			char m_header[5];

			void send(const util::span<const char>& data);

		public:
			network_sink(std::unique_ptr<sink> next, util::shared_wsa wsa, const std::string& bind_address, const std::string& peer_address, const std::string& peer_service);

			virtual bool can_play() const;

			virtual bool is_playing() const;

			virtual bool process(const float* samples, size_t frames);

			static size_t adjust_samplerate(size_t samplerate);
		};
	}
}