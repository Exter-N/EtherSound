#pragma once

#include <WinSock2.h>
#include <memory>
#include <vector>

#include "nn.hpp"
#include "no_copy.h"
#include "span.h"

namespace wascap
{
	namespace util
	{
		class wsa;

		typedef dropbox::oxygen::nn_shared_ptr<wsa> shared_wsa;

		shared_wsa make_shared_wsa();

		class wsa
		{
			WSADATA m_wsa_data;

		protected:
			wsa();

		public:
			~wsa();

			inline const WSADATA& operator *() const { return m_wsa_data; }
			inline const WSADATA* operator ->() const { return &m_wsa_data; }

			inline WSADATA& operator *() { return m_wsa_data; }
			inline WSADATA* operator ->() { return &m_wsa_data; }

			friend shared_wsa make_shared_wsa();
		};

		class wsa_addrinfo
		{
			shared_wsa m_wsa;
			addrinfo* m_info;

		public:
			wsa_addrinfo(shared_wsa wsa, const char* node_name, const char* service_name, const addrinfo& hints);
			~wsa_addrinfo();

			inline addrinfo* get() const { return m_info; }

			span<char> addr() const;

			inline operator addrinfo*() const { return m_info; }

			inline addrinfo& operator *() const { return *m_info; }
			inline addrinfo* operator ->() const { return m_info; }
		};

		class wsa_socket : public no_copy
		{
			shared_wsa m_wsa;
			SOCKET m_socket;

		public:
			inline explicit wsa_socket(shared_wsa wsa) : m_wsa(wsa), m_socket(-1) { }
			wsa_socket(shared_wsa wsa, int af, int type, int protocol);
			inline wsa_socket(shared_wsa wsa, SOCKET socket) : m_wsa(wsa), m_socket(socket) { }
			wsa_socket(wsa_socket&& other);
			~wsa_socket();

			void reset();
			void swap(wsa_socket& other);

			void bind(const span<const char>& addr);
			int sendto(const span<const char>& data, int flags, const span<const char>& to);

			inline SOCKET get() const { return m_socket; }

			inline operator SOCKET() const { return m_socket; }

			wsa_socket& operator =(wsa_socket&& socket);
		};
	}
}
