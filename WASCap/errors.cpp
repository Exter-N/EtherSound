#include "stdafx.h"

#include <memory>
#include <WinSock2.h>
#include <comdef.h>

#include "win32_helper.h"
#include "errors.h"

namespace
{
	class win32_category_t : public std::error_category
	{
	public:
		virtual const char* name() const noexcept
		{
			return "win32";
		}

		virtual std::string message(int code) const
		{
			LPSTR message_buffer = nullptr;
			size_t size = FormatMessageA(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, nullptr, code, 0, (LPSTR)&message_buffer, 0, nullptr);
			std::unique_ptr<char[], wascap::util::local_deleter<char[]>> message_buffer_u(message_buffer);
			std::string message(message_buffer_u.get(), size - 2);

			return message;
		}
	};

	class com_category_t : public std::error_category
	{
	public:
		virtual const char* name() const noexcept
		{
			return "com";
		}

		virtual std::string message(int code) const
		{
			_com_error err(code);
			const TCHAR* message = err.ErrorMessage();

#if UNICODE
			size_t size;
			wcstombs_s(&size, nullptr, 0, message, 0);
			std::unique_ptr<char[]> buffer = std::make_unique<char[]>(size);
			wcstombs_s(&size, buffer.get(), size, message, size);
			std::string str_message(buffer.get(), size - 1);

			return str_message;
#else
			return std::string(message);
#endif
		}
	};

	win32_category_t win32_cat;
	com_category_t com_cat;
}

const std::error_category& wascap::util::win32_category() noexcept { return win32_cat; }
const std::error_category& wascap::util::com_category() noexcept { return com_cat; }

std::error_code wascap::util::win32_last_error() noexcept
{
	return std::error_code(GetLastError(), win32_category());
}

std::error_code wascap::util::wsa_last_error() noexcept
{
	return std::error_code(WSAGetLastError(), win32_category());
}

std::error_code wascap::util::com_error(HRESULT hr) noexcept
{
	switch (HRESULT_FACILITY(hr)) {
	case FACILITY_WIN32:
		return std::error_code(HRESULT_CODE(hr), win32_category());
	default:
		return std::error_code(hr, com_category());
	}
}
