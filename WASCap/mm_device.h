#pragma once

#include <mmdeviceapi.h>
#include <string>
#include <vector>

#include "com_helper.h"

namespace wascap
{
	namespace was
	{
		inline DWORD channel_mask(const WAVEFORMATEX& format)
		{
			return (format.wFormatTag == WAVE_FORMAT_EXTENSIBLE) ? ((const WAVEFORMATEXTENSIBLE&)format).dwChannelMask : ((1U << format.nChannels) - 1);
		}

		class mm_device
		{
			util::com_ptr<IMMDevice> m_device;

			std::string string_property(const PROPERTYKEY& key) const;

		public:
			mm_device(const util::com_ptr<IMMDevice>& device);
			mm_device(util::com_ptr<IMMDevice>&& device);

			EDataFlow data_flow() const;

			DWORD state() const;

			std::string id() const;

			std::string friendly_name() const;

			void mix_format(size_t& samplerate, DWORD& channel_mask) const;

			inline size_t samplerate() const
			{
				size_t samplerate;
				DWORD channel_mask;

				mix_format(samplerate, channel_mask);

				return samplerate;
			}

			inline DWORD channel_mask() const
			{
				size_t samplerate;
				DWORD channel_mask;

				mix_format(samplerate, channel_mask);

				return channel_mask;
			}

			template<typename T>
			inline util::com_ptr<T> activate(DWORD dwClsCtx, PROPVARIANT* pActivationParams) const
			{
				util::com_ptr<T> instance;
				COM_CHECK(m_device->Activate(__uuidof(T), dwClsCtx, pActivationParams, instance.ppv()));

				return instance;
			}

			inline IMMDevice* operator->() const { return m_device.get(); }
		};

		class mm_enumerator
		{
			util::com_ptr<IMMDeviceEnumerator> m_enumerator;

		public:
			mm_enumerator(const util::shared_com& com);

			mm_device default_device(EDataFlow dataFlow, ERole role) const;

			std::vector<mm_device> all_devices(EDataFlow dataFlow, DWORD dwStateMask) const;

			mm_device device_by_id(const std::string& id) const;

			inline IMMDeviceEnumerator* operator->() const { return m_enumerator.get(); }
		};
	}
}