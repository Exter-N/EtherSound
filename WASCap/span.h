#pragma once

#include <algorithm>
#include <array>
#include <cstring>
#include <stdexcept>
#include <string>
#include <type_traits>
#include <vector>

#include "string_format.h"

namespace wascap
{
	namespace util
	{
		constexpr size_t npos = static_cast<size_t>(-1);

		template<typename T>
		class span
		{
			T* m_ptr;
			size_t m_size;

		public:
			inline span(T* ptr, size_t size) : m_ptr(ptr), m_size(size) { }
			template<typename Alloc = std::allocator<T>>
			inline span(std::vector<T, Alloc>& vec) : m_ptr(&vec[0]), m_size(vec.size()) { }
			template<typename Traits = std::char_traits<T>, typename Alloc = std::allocator<T>>
			inline span(std::basic_string<T, Traits, Alloc>& str) : m_ptr(&str[0]), m_size(str.size()) { }
			template<size_t size>
			inline span(T data[size]) : m_ptr(data), m_size(size) { }
			template<size_t size>
			inline span(std::array<T, size> data) : m_ptr(&data[0]), m_size(size) { }

			template<typename U>
			inline span(const span<U>& other): m_ptr(other.get()), m_size(other.size()) { }

			inline T* get() const { return m_ptr; }
			inline size_t size() const { return m_size; }

			inline T* begin() const { return m_ptr; }
			inline T* end() const { return m_ptr + m_size; }

			inline T& at(size_t i) const
			{
				if (i >= m_size) {
					throw std::out_of_range(string_format("Index %d out of range (span size = %d)", i, m_size));
				}

				return m_ptr[i];
			}

			inline span<T> subspan(size_t start, size_t size = npos) const
			{
				if (start > m_size) {
					throw std::out_of_range(string_format("Starting position %d out of range (span size = %d)", start, m_size));
				}
				if (npos == size) {
					size = m_size - start;
				}
				if (start + size > m_size) {
					throw std::out_of_range(string_format("Ending position %d out of range (span size = %d)", start + size, m_size));
				}

				return span<T>(m_ptr + start, size);
			}

			inline T& operator *() const { return *m_ptr; }
			inline T* operator ->() const { return m_ptr; }
			inline T& operator [](size_t i) const { return m_ptr[i]; }
		};

		template<typename T>
		inline span<T> make_span(T* ptr, size_t size)
		{
			return span<T>(ptr, size);
		}

		template<typename T, typename Alloc = std::allocator<T>>
		inline span<T> make_span(std::vector<T, Alloc>& vec)
		{
			return span<T>(vec);
		}

		template<typename T, typename Traits = std::char_traits<T>, typename Alloc = std::allocator<T>>
		inline span<T> make_span(std::basic_string<T, Traits, Alloc>& str)
		{
			return span<T>(str);
		}

		template<typename T, std::enable_if_t<std::is_trivially_copyable_v<T>, bool> = true>
		inline span<T>& operator <<(span<T>& dest, const span<T>& source)
		{
			if (source.size() != dest.size()) {
				throw std::logic_error(string_format("Span size mismatch (dest size = %d, source size = %d)", dest.size(), source.size()));
			}

			memcpy(dest.get(), source.get(), dest.size() * sizeof(T));

			return dest;
		}

		template<typename T, std::enable_if_t<!std::is_trivially_copyable_v<T>, bool> = true>
		inline span<T>& operator <<(span<T>& dest, const span<T>& source)
		{
			if (source.size() != dest.size()) {
				throw std::logic_error(string_format("Span size mismatch (dest size = %d, source size = %d)", dest.size(), source.size()));
			}

			std::copy(source.begin(), source.end(), dest.begin());

			return dest;
		}

		template<typename T, typename Alloc = std::allocator<T>>
		std::vector<T, Alloc>& operator <<(std::vector<T, Alloc>& to, const span<T>& from)
		{
			to.resize(from.size());
			make_span(to) << from;

			return to;
		}

		template<typename T, typename Traits = std::char_traits<T>, typename Alloc = std::allocator<T>>
		std::basic_string<T, Traits, Alloc>& operator <<(std::basic_string<T, Traits, Alloc>& to, const span<T>& from)
		{
			to.resize(from.size());
			make_span(to) << from;

			return to;
		}
	}
}