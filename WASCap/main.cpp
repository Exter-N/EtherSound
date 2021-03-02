#include "stdafx.h"

#include <windows.h>
#include <audioclient.h>
#include <mmdeviceapi.h>
#include <sstream>
#include <memory>

#include "convert_sink.h"
#include "com_helper.h"
#include "errors.h"
#include "main.h"
#include "mm_device.h"
#include "network_sink.h"
#include "shmctl_sink.h"
#include "stdout_sink.h"
#include "was_source.h"
#include "was_sink.h"
#include "string_format.h"

namespace
{
	const char* stringify_flow(EDataFlow flow)
	{
		switch (flow) {
		case eRender:
			return "render";
		case eCapture:
			return "capture";
		case eAll:
			return "all";
		default:
			return "?";
		}
	}

	const char* stringify_role(ERole role)
	{
		switch (role) {
		case eConsole:
			return "console";
		case eMultimedia:
			return "multimedia";
		case eCommunications:
			return "communications";
		default:
			return "?";
		}
	}

	const char* stringify_state_mask(DWORD state_mask)
	{
		switch (state_mask) {
		case DEVICE_STATE_ACTIVE:
			return "active";
		case DEVICE_STATE_DISABLED:
			return "disabled";
		case DEVICE_STATE_NOTPRESENT:
			return "not-present";
		case DEVICE_STATE_UNPLUGGED:
			return "unplugged";
		default:
			return "?";
		}
	}

	std::string stringify_defaults(const std::vector<std::string>& defaults, size_t flow_offset, const std::string& id)
	{
		std::ostringstream defs;
		bool first = true;
		for (size_t role = 0; role < ERole_enum_count; ++role) {
			if (defaults.at(flow_offset + role) == id) {
				if (first) {
					first = false;
				}
				else {
					defs << ' ';
				}
				defs << stringify_role((ERole)role);
			}
		}

		return defs.str();
	}
}

DWORD WINAPI wascap::bind_lifetime(HANDLE hProcess)
{
	ExitProcess((WAIT_FAILED == WaitForSingleObject(hProcess, INFINITE)) ? 1 : 0);

	return 0;
}

int wascap::list_main(const command_line_arguments& arguments)
{
	if (arguments.use_message_box) {
		MessageBoxA(nullptr, util::string_format("Initializing WASCap list (PID %d)", GetCurrentProcessId()).c_str(), "WASCap", MB_ICONINFORMATION);
	}

	util::shared_com com = util::make_shared_com();

	was::mm_enumerator enumerator(com);

	std::vector<std::string> defaults;
	defaults.reserve(ERole_enum_count * ((eAll == arguments.list_flow) ? 2 : 1));

	if (eRender == arguments.list_flow || eAll == arguments.list_flow) {
		for (size_t role = 0; role < ERole_enum_count; ++role) {
			defaults.push_back(enumerator.default_device(eRender, (ERole)role).id());
		}
	}
	if (eCapture == arguments.list_flow || eAll == arguments.list_flow) {
		for (size_t role = 0; role < ERole_enum_count; ++role) {
			defaults.push_back(enumerator.default_device(eCapture, (ERole)role).id());
		}
	}

	std::vector<was::mm_device> devices = enumerator.all_devices(arguments.list_flow, arguments.list_state_mask);

	if (arguments.use_message_box) {
		std::ostringstream message;
		bool first = true;
		for (const was::mm_device& device : devices) {
			if (first) {
				first = false;
			}
			else {
				message << "\n\n";
			}
			std::string id = device.id();
			EDataFlow flow = device.data_flow();
			DWORD state = device.state();
			message << util::string_format("%s\n%s\n%s %s", id, device.friendly_name(), stringify_flow(flow), stringify_state_mask(state));
			size_t flow_offset = (eAll == arguments.list_flow) ? ((size_t)flow * (size_t)ERole_enum_count) : 0;
			std::string defs = stringify_defaults(defaults, flow_offset, id);
			if (!defs.empty()) {
				message << util::string_format("\nDefault for %s", defs);
			}
			if (state == DEVICE_STATE_ACTIVE) {
				size_t samplerate;
				DWORD channel_mask;
				device.mix_format(samplerate, channel_mask);
				message << util::string_format("\n%d Hz, %d ch (%08x)", samplerate, __popcnt(channel_mask), channel_mask);
			}
		}
		MessageBoxA(nullptr, message.str().c_str(), "WASCap", MB_ICONINFORMATION);
	}
	else {
		std::ostringstream buffer;
		buffer << 7 << '\n';
		for (const was::mm_device& device : devices) {
			std::string id = device.id();
			EDataFlow flow = device.data_flow();
			DWORD state = device.state();
			size_t samplerate;
			DWORD channel_mask;
			if (state == DEVICE_STATE_ACTIVE) {
				device.mix_format(samplerate, channel_mask);
			}
			else {
				samplerate = 0;
				channel_mask = 0;
			}
			size_t flow_offset = (eAll == arguments.list_flow) ? ((size_t)flow * (size_t)ERole_enum_count) : 0;
			std::string defs = stringify_defaults(defaults, flow_offset, id);
			buffer << id << '\n' << device.friendly_name() << '\n' << stringify_flow(flow) << '\n' << stringify_state_mask(state) << '\n' << samplerate << '\n' << channel_mask << '\n' << defs << '\n';
		}

		std::string data = buffer.str();
		fwrite(data.data(), sizeof(char), data.size(), stdout);
	}

	return 1;
}

int wascap::capture_main(const command_line_arguments& arguments)
{
	if (arguments.use_message_box) {
		MessageBoxA(nullptr, util::string_format("Initializing WASCap capture (PID %d)", GetCurrentProcessId()).c_str(), "WASCap", MB_ICONINFORMATION);
	}
	else {
		fprintf(stderr, "Initializing WASCap capture (PID %d)\n", GetCurrentProcessId());
	}

	util::shared_com com = util::make_shared_com();

	was::mm_enumerator enumerator(com);

	was::mm_device source_dev = arguments.source_device.empty()
		? enumerator.default_device(arguments.source_flow, arguments.source_role)
		: enumerator.device_by_id(arguments.source_device);

	source::was_source source(source_dev);

	const WAVEFORMATEX& format = source.wave_format();
	DWORD channel_mask = was::channel_mask(format);

	size_t chain_samplerate = (arguments.samplerate != SIZE_MAX) ? arguments.samplerate : format.nSamplesPerSec;
	DWORD chain_channel_mask = (arguments.channel_mask != 0) ? arguments.channel_mask : channel_mask;

	size_t before_was_samplerate = arguments.with_network_sink ? sink::network_sink::adjust_samplerate(chain_samplerate) : chain_samplerate;

	std::unique_ptr<sink::sink> s;

	if (arguments.with_was_sink) {
		was::mm_device sink_dev = arguments.sink_device.empty()
			? enumerator.default_device(eRender, arguments.sink_role)
			: enumerator.device_by_id(arguments.sink_device);
		size_t sink_samplerate = sink_dev.samplerate();

		s = std::make_unique<sink::null_sink>(sink_samplerate, chain_channel_mask);
		s = std::make_unique<sink::was_sink>(std::move(s), sink_dev);
		if (before_was_samplerate != s->samplerate()) {
			s = std::make_unique<sink::samplerate_convert_sink>(std::move(s), before_was_samplerate);
		}
	}
	else {
		s = std::make_unique<sink::null_sink>(before_was_samplerate, chain_channel_mask);
	}

	if (arguments.with_network_sink) {
		util::shared_wsa wsa = util::make_shared_wsa();

		s = std::make_unique<sink::network_sink>(std::move(s), wsa, arguments.bind_address, arguments.peer_address, arguments.peer_service);
	}

	if (chain_samplerate != s->samplerate()) {
		s = std::make_unique<sink::samplerate_convert_sink>(std::move(s), chain_samplerate);
	}

	if (arguments.with_stdout_sink) {
		s = std::make_unique<sink::stdout_sink>(std::move(s));
	}

	if (!arguments.shm_name.empty()) {
		std::shared_ptr<shmctl::shmctl> shmctl = std::make_shared<shmctl::shmctl>(arguments.shm_name);

		if (arguments.with_shm_tap_sink) {
			s = std::make_unique<sink::shmctl_tap_sink>(std::move(s), shmctl);
		}
		s = std::make_unique<sink::shmctl_volume_sink>(std::move(s), shmctl);
		if (arguments.with_shm_averaging_sink) {
			s = std::make_unique<sink::shmctl_averaging_sink>(std::move(s), shmctl);
		}
		s = std::make_unique<sink::shmctl_flow_control_sink>(std::move(s), shmctl);
	}

	if (format.nSamplesPerSec != s->samplerate()) {
		s = std::make_unique<sink::samplerate_convert_sink>(std::move(s), format.nSamplesPerSec);
	}
	if (channel_mask != s->channel_mask()) {
		s = std::make_unique<sink::channel_convert_sink>(std::move(s), channel_mask);
	}

	if (!s->can_play()) {
		throw bad_arguments("Unable to play");
	}

	SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_TIME_CRITICAL);

	fprintf(stderr, "WASCap capture initialized\n");

	if (arguments.duration == INFINITY) {
		while (s->is_open()) {
			if (s->is_playing()) {
				source.run(*s, format.nSamplesPerSec * 3600);
			}
			else {
				Sleep(6);
			}
		}
	}
	else {
		if (s->is_open() && s->is_playing()) {
			source.run(*s, (size_t)(format.nSamplesPerSec * arguments.duration));
		}
	}

	return 0;
}
