#pragma once

#include <memory>
#include <string>
#include <stdexcept>
#include <type_traits>

namespace wascap
{
	namespace util
	{
		namespace internal
		{
			template<typename T, std::enable_if_t<std::is_pointer_v<T> || std::is_fundamental_v<T>, bool> = true>
			inline typename T string_format_convert(T value)
			{
				return value;
			}

			inline const char* string_format_convert(const std::string& value)
			{
				return value.c_str();
			}
		}

		// Inspired from https://stackoverflow.com/a/26221725/9817312

		template<typename ... Args>
		std::string string_format(const std::string& format, Args&& ... args)
		{
			int size = snprintf(nullptr, 0, format.c_str(), internal::string_format_convert(std::forward<Args>(args)) ...) + 1; // Extra space for '\0'
			if (size <= 0) { throw std::runtime_error("Error during formatting."); }
			std::unique_ptr<char[]> buf = std::make_unique<char[]>(size);
			snprintf(buf.get(), size, format.c_str(), internal::string_format_convert(std::forward<Args>(args)) ...);
			return std::string(buf.get(), buf.get() + size - 1); // We don't want the '\0' inside
		}

		template<typename ... Args>
		std::unique_ptr<wchar_t[]> wstr_format(const wchar_t* format, Args&& ... args)
		{
			int size = swprintf(nullptr, 0, format, internal::string_format_convert(std::forward<Args>(args)) ...) + 1;
			if (size <= 0) { throw std::runtime_error("Error during formatting."); }
			std::unique_ptr<wchar_t[]> buf = std::make_unique<wchar_t[]>(size);
			swprintf(buf.get(), size, format, internal::string_format_convert(std::forward<Args>(args)) ...);
			return buf;
		}

		std::string string_from_wstr(const wchar_t* wstr);
		std::unique_ptr<wchar_t[]> wstr_from_string(const char* str);
		std::unique_ptr<wchar_t[]> wstr_from_string(const std::string& str);
	}
}
