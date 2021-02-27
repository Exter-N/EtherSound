#pragma once

#include <memory>

#include "nn.hpp"
#include "errors.h"
#include "no_copy.h"

namespace wascap
{
	namespace util
	{
		template<typename T>
		class com_ptr
		{
			T* m_ptr;

		public:
			inline com_ptr(): m_ptr(nullptr) { }
			inline explicit com_ptr(T* ptr) : m_ptr(ptr) { }
			inline com_ptr(const com_ptr<T>& ptr) : m_ptr(ptr.m_ptr)
			{
				if (m_ptr) {
					m_ptr->AddRef();
				}
			}
			inline com_ptr(com_ptr<T>&& ptr) noexcept : m_ptr(ptr.m_ptr)
			{
				ptr.m_ptr = nullptr;
			}
			inline ~com_ptr()
			{
				reset();
			}

			inline T* get() const { return m_ptr; }

			inline T** ppi() { return &m_ptr; }
			inline void** ppv() { return (void**)&m_ptr; }

			inline void reset()
			{
				if (m_ptr) {
					m_ptr->Release();
					m_ptr = nullptr;
				}
			}
			inline void swap(com_ptr<T>& ptr)
			{
				std::swap(m_ptr, ptr.m_ptr);
			}

			template<typename U>
			inline const com_ptr<U> as() const
			{
				if (!m_ptr) {
					return com_ptr<U>(nullptr);
				}

				com_ptr<U> ptr;
				COM_CHECK(m_ptr->QueryInterface(__uuidof(U), ptr.ppv()));

				return ptr;
			}

			template<typename U>
			inline com_ptr<U> as()
			{
				if (!m_ptr) {
					return com_ptr<U>(nullptr);
				}

				com_ptr<U> ptr;
				COM_CHECK(m_ptr->QueryInterface(__uuidof(U), ptr.ppv()));

				return ptr;
			}

			inline com_ptr<T>& operator =(const com_ptr<T>& ptr)
			{
				if (m_ptr) {
					m_ptr->Release();
				}
				m_ptr = ptr.m_ptr;
				if (m_ptr) {
					m_ptr->AddRef();
				}

				return *this;
			}
			inline com_ptr<T>& operator =(com_ptr<T>&& ptr)
			{
				if (m_ptr) {
					m_ptr->Release();
				}
				m_ptr = ptr.m_ptr;
				ptr.m_ptr = nullptr;

				return *this;
			}

			inline operator T*() const { return m_ptr; }

			inline T& operator *() const { return *m_ptr; }
			inline T* operator ->() const { return m_ptr; }
			inline operator bool() const { return (bool)m_ptr; }
		};

		template<typename T>
		inline com_ptr<T>&& make_com(T* ptr)
		{
			return com_ptr(ptr);
		}

		class com_prop_variant : public no_copy_no_move
		{
			PROPVARIANT m_var;

		public:
			com_prop_variant();
			~com_prop_variant();

			inline const PROPVARIANT& get() const { return m_var; }
			inline PROPVARIANT& get() { return m_var; }

			inline const PROPVARIANT* p() const { return &m_var; }
			inline PROPVARIANT* p() { return &m_var; }

			inline const PROPVARIANT* operator ->() const { return &m_var; }
			inline PROPVARIANT* operator ->() { return &m_var; }
		};

		template<typename T>
		struct co_task_mem_deleter
		{
			inline void operator()(T* ptr) { if (nullptr != ptr) { CoTaskMemFree(ptr); } }
		};

		template<typename T>
		struct co_task_mem_deleter<T[]>
		{
			inline void operator()(T* ptr) { if (nullptr != ptr) { CoTaskMemFree(ptr); } }
		};

		template<typename T>
		using co_task_unique_ptr = std::unique_ptr<T, co_task_mem_deleter<T>>;

		class com;

		typedef dropbox::oxygen::nn_shared_ptr<com> shared_com;

		shared_com make_shared_com();

		class com : public no_copy_no_move
		{
		protected:
			com();

		public:
			~com();

			template<typename T>
			inline com_ptr<T> create_instance(REFCLSID rclsid, LPUNKNOWN pUnkOuter, DWORD dwClsContext) const
			{
				com_ptr<T> instance;
				COM_CHECK(CoCreateInstance(rclsid, pUnkOuter, dwClsContext, __uuidof(T), instance.ppv()));

				return instance;
			}

			friend shared_com make_shared_com();
		};
	}
}
