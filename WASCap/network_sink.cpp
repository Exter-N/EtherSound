#include "stdafx.h"

#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>

#include "network_sink.h"
#include "wsa_helper.h"
#include "errors.h"
#include "string_format.h"

#define DEFAULT_PEER_ADDRESS "239.255.77.77"
#define DEFAULT_PEER_SERVICE "4010"
#define MAX_PAYLOAD_SAMPLES 288

namespace
{
	char samplerate_header(size_t samplerate)
	{
		size_t multiplier;
		if ((samplerate % 48000) == 0) {
			multiplier = samplerate / 48000;
			if (multiplier > 127) {
				throw std::domain_error(wascap::util::string_format("Invalid samplerate %d Hz (48 kHz * %d)\n", samplerate, multiplier));
			}

			return (char)multiplier;
		}
		else if ((samplerate % 44100) == 0) {
			multiplier = samplerate / 44100;
			if (multiplier > 127) {
				throw std::domain_error(wascap::util::string_format("Invalid samplerate %d Hz (44.1 kHz * %d)\n", samplerate, multiplier));
			}

			return (char)(multiplier | 128);
		}
		else {
			throw std::domain_error(wascap::util::string_format("Invalid samplerate %d (unrecognized base rate)\n", samplerate));
		}
	}
}

wascap::sink::network_sink::network_sink(std::unique_ptr<sink> next, util::shared_wsa wsa, const std::string& bind_address, const std::string& peer_address, const std::string& peer_service)
	: chain_sink(std::move(next)), m_wsa(wsa), m_socket(wsa), m_peername()
{
	m_header[0] = samplerate_header(samplerate());
	m_header[1] = 32;
	m_header[2] = (char)channels();
	m_header[3] = (char)(channel_mask() >> 8);
	m_header[4] = (char)channel_mask();

	struct addrinfo hints = { 0 };

	hints.ai_family = AF_UNSPEC;
	hints.ai_socktype = SOCK_DGRAM;
	hints.ai_protocol = IPPROTO_UDP;

	{
		util::wsa_addrinfo peer_addr(wsa, peer_address.empty() ? DEFAULT_PEER_ADDRESS : peer_address.c_str(), peer_service.empty() ? DEFAULT_PEER_SERVICE : peer_service.c_str(), hints);
		m_socket = util::wsa_socket(wsa, peer_addr->ai_family, peer_addr->ai_socktype, peer_addr->ai_protocol);
		m_peername << peer_addr.addr();
	}

	if (!bind_address.empty()) {
		util::wsa_addrinfo bind_addr(wsa, bind_address.c_str(), "0", hints);
		m_socket.bind(bind_addr.addr());
	}
}

void wascap::sink::network_sink::send(const util::span<const char>& data)
{
	m_socket.sendto(data, 0, util::make_span(m_peername));
}

bool wascap::sink::network_sink::can_play() const
{
	return true;
}

bool wascap::sink::network_sink::is_playing() const
{
	return true;
}

bool wascap::sink::network_sink::process(const float* samples, size_t frames)
{
	size_t max_samples = MAX_PAYLOAD_SAMPLES - MAX_PAYLOAD_SAMPLES % channels();
	const float* cur_samples = samples;
	size_t n_samples = frames * channels();

	char buffer[sizeof(m_header) + (MAX_PAYLOAD_SAMPLES << 2)];
	memcpy(buffer, m_header, sizeof(m_header));
	while (n_samples > max_samples) {
		memcpy(buffer + sizeof(m_header), (const char*)cur_samples, max_samples << 2);
		send(util::make_span(buffer, sizeof(m_header) + (max_samples << 2)));
		cur_samples += max_samples;
		n_samples -= max_samples;
	}
	if (n_samples > 0) {
		memcpy(buffer + sizeof(m_header), (const char*)cur_samples, n_samples << 2);
		send(util::make_span(buffer, sizeof(m_header) + (n_samples << 2)));
	}

	return chain_sink::process(samples, frames);
}

size_t wascap::sink::network_sink::adjust_samplerate(size_t samplerate)
{
	if ((samplerate % 48000) == 0 || (samplerate % 44100) == 0) {
		return samplerate;
	}

	return 48000;
}