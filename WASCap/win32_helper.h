#pragma once

#include <Windows.h>

namespace wascap
{
	namespace util
	{
		template<typename T>
		struct local_deleter
		{
			inline void operator()(T* ptr) { if (nullptr != ptr) { LocalFree(ptr); } }
		};

		template<typename T>
		struct local_deleter<T[]>
		{
			inline void operator()(T* ptr) { if (nullptr != ptr) { LocalFree(ptr); } }
		};
	}
}