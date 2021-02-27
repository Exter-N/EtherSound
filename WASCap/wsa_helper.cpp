#include "stdafx.h"

#include <ws2tcpip.h>

#include "wsa_helper.h"
#include "errors.h"

#pragma comment (lib, "ws2_32.lib")

wascap::util::wsa::wsa() : m_wsa_data()
{
	WSA_CHECK(WSAStartup(MAKEWORD(2, 2), &m_wsa_data));
}

wascap::util::wsa::~wsa()
{
	WSACleanup();
}

wascap::util::shared_wsa wascap::util::make_shared_wsa()
{
	class concrete_wsa : public wsa { };

	return dropbox::oxygen::nn_make_shared<concrete_wsa>();
}

wascap::util::wsa_addrinfo::wsa_addrinfo(shared_wsa wsa, const char* node_name, const char* service_name, const addrinfo& hints)
	: m_wsa(wsa), m_info(nullptr)
{
	WSA_CHECK(getaddrinfo(node_name, service_name, &hints, &m_info));
}

wascap::util::wsa_addrinfo::~wsa_addrinfo()
{
	freeaddrinfo(m_info);
}

wascap::util::span<char> wascap::util::wsa_addrinfo::addr() const
{
	return make_span((char*)m_info->ai_addr, m_info->ai_addrlen);
}

wascap::util::wsa_socket::wsa_socket(shared_wsa wsa, int af, int type, int protocol)
	: m_wsa(wsa), m_socket(WSA_CHECK_U(socket(af, type, protocol)))
{
}

wascap::util::wsa_socket::wsa_socket(wsa_socket&& other)
	: m_wsa(other.m_wsa), m_socket(other.m_socket)
{
	other.m_socket = -1;
}

wascap::util::wsa_socket::~wsa_socket()
{
	if (-1 != m_socket) {
		closesocket(m_socket);
	}
}

void wascap::util::wsa_socket::reset()
{
	if (-1 != m_socket) {
		closesocket(m_socket);
		m_socket = -1;
	}
}

void wascap::util::wsa_socket::swap(wsa_socket& other)
{
	std::swap(m_socket, other.m_socket);
}

void wascap::util::wsa_socket::bind(const span<const char>& addr)
{
	WSA_CHECK(::bind(m_socket, (const sockaddr*)addr.get(), addr.size()));
}

int wascap::util::wsa_socket::sendto(const span<const char>& data, int flags, const span<const char>& to)
{
	return WSA_CHECK_U(::sendto(m_socket, data.get(), data.size(), flags, (const sockaddr*)to.get(), to.size()));
}

wascap::util::wsa_socket& wascap::util::wsa_socket::operator =(wsa_socket&& other)
{
	if (-1 != m_socket) {
		closesocket(m_socket);
	}
	m_socket = other.m_socket;
	other.m_socket = -1;

	return *this;
}
