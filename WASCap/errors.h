#pragma once

#include <system_error>
#include <Windows.h>

#define WIN32_CHECK(win32_op) (::wascap::util::win32_check((win32_op), #win32_op))

#define WSA_CHECK(wsa_op) (::wascap::util::wsa_check((wsa_op), #wsa_op))
#define WSA_CHECK_U(wsa_op) (::wascap::util::wsa_check_u((wsa_op), #wsa_op))

#define COM_CHECK(com_op) (::wascap::util::com_check((com_op), #com_op))

namespace wascap
{
	namespace util
	{
		const std::error_category& win32_category() noexcept;
		std::error_code win32_last_error() noexcept;
		std::error_code wsa_last_error() noexcept;

		const std::error_category& com_category() noexcept;
		std::error_code com_error(HRESULT hr) noexcept;

		template<typename T>
		inline T win32_check(T retval, const char* op)
		{
			if (!retval) {
				throw std::system_error(win32_last_error(), op);
			}

			return retval;
		}

		inline void wsa_check(INT retval, const char* op)
		{
			if (0 != retval) {
				throw std::system_error(wsa_last_error(), op);
			}
		}

		inline INT wsa_check_u(INT retval, const char* op)
		{
			if (retval < 0) {
				throw std::system_error(wsa_last_error(), op);
			}

			return retval;
		}

		inline HRESULT com_check(HRESULT hr, const char* op)
		{
			if (FAILED(hr)) {
				throw std::system_error(com_error(hr), op);
			}

			return hr;
		}
	}
}
