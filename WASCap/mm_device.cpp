#include "stdafx.h"

#include <Audioclient.h>
#include <utility>

#include "mm_device.h"
#include "errors.h"
#include "string_format.h"

#include <Functiondiscoverykeys_devpkey.h>

wascap::was::mm_device::mm_device(const wascap::util::com_ptr<IMMDevice>& device)
	: m_device(device)
{
}

wascap::was::mm_device::mm_device(wascap::util::com_ptr<IMMDevice>&& device)
	: m_device(device)
{
}

std::string wascap::was::mm_device::string_property(const PROPERTYKEY& key) const
{
	util::com_ptr<IPropertyStore> props;
	COM_CHECK(m_device->OpenPropertyStore(STGM_READ, props.ppi()));

	util::com_prop_variant var;
	COM_CHECK(props->GetValue(key, var.p()));
	if (var->vt != VT_LPWSTR) {
		throw std::logic_error("invalid string property");
	}
	
	return util::string_from_wstr(var->pwszVal);
}

EDataFlow wascap::was::mm_device::data_flow() const
{
	EDataFlow data_flow;
	COM_CHECK(m_device.as<IMMEndpoint>()->GetDataFlow(&data_flow));

	return data_flow;
}

DWORD wascap::was::mm_device::state() const
{
	DWORD state;
	COM_CHECK(m_device->GetState(&state));

	return state;
}

std::string wascap::was::mm_device::id() const
{
	LPWSTR id;
	COM_CHECK(m_device->GetId(&id));
	util::co_task_unique_ptr<wchar_t[]> id_u(id);
	std::string id_str = util::string_from_wstr(id_u.get());

	return id_str;
}

std::string wascap::was::mm_device::friendly_name() const
{
	return string_property(PKEY_Device_FriendlyName);
}

void wascap::was::mm_device::mix_format(size_t& samplerate, DWORD& channel_mask) const
{
	util::co_task_unique_ptr<WAVEFORMATEX> wave_format;
	{
		util::com_ptr<IAudioClient> audio_client = activate<IAudioClient>(CLSCTX_ALL, nullptr);

		WAVEFORMATEX* pwfx;
		COM_CHECK(audio_client->GetMixFormat(&pwfx));
		wave_format.reset(pwfx);
	}

	samplerate = wave_format->nSamplesPerSec;
	channel_mask = was::channel_mask(*wave_format);
}

wascap::was::mm_enumerator::mm_enumerator(const util::shared_com& com)
	: m_enumerator(com->create_instance<IMMDeviceEnumerator>(__uuidof(MMDeviceEnumerator), nullptr, CLSCTX_ALL))
{
}

wascap::was::mm_device wascap::was::mm_enumerator::default_device(EDataFlow dataFlow, ERole role) const
{
	util::com_ptr<IMMDevice> device;
	COM_CHECK(m_enumerator->GetDefaultAudioEndpoint(dataFlow, role, device.ppi()));

	return mm_device(std::move(device));
}

std::vector<wascap::was::mm_device> wascap::was::mm_enumerator::all_devices(EDataFlow dataFlow, DWORD dwStateMask) const
{
	util::com_ptr<IMMDeviceCollection> devices;
	COM_CHECK(m_enumerator->EnumAudioEndpoints(dataFlow, dwStateMask, devices.ppi()));

	UINT n_devices;
	COM_CHECK(devices->GetCount(&n_devices));

	std::vector<mm_device> devices_v;
	devices_v.reserve(n_devices);

	for (UINT i = 0; i < n_devices; ++i) {
		util::com_ptr<IMMDevice> device;
		COM_CHECK(devices->Item(i, device.ppi()));
		devices_v.push_back(mm_device(std::move(device)));
	}

	return devices_v;
}

wascap::was::mm_device wascap::was::mm_enumerator::device_by_id(const std::string& id) const
{
	util::com_ptr<IMMDevice> device;
	{
		std::unique_ptr<wchar_t[]> id_w = util::wstr_from_string(id);
		COM_CHECK(m_enumerator->GetDevice(id_w.get(), device.ppi()));
	}

	return mm_device(std::move(device));
}