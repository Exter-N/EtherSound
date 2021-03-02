#include "stdafx.h"

#include <intrin.h>
#include <sstream>

#include "main.h"
#include "base_sink.h"
#include "string_format.h"

namespace
{
	inline void parse_assert(bool assertion, const char* failure_what)
	{
		if (!assertion) {
			throw wascap::bad_arguments(failure_what);
		}
	}

	inline void parse_assert(bool assertion, const std::string& failure_what)
	{
		if (!assertion) {
			throw wascap::bad_arguments(failure_what);
		}
	}

	wascap::verb parse_verb(const std::string& word)
	{
		if (word == "help") {
			return wascap::help;
		}
		else if (word == "list") {
			return wascap::list;
		}
		else if (word == "capture") {
			return wascap::capture;
		}
		else {
			throw wascap::bad_arguments(wascap::util::string_format("Unrecognized verb: %s", word));
		}
	}

	EDataFlow parse_flow(const std::string& word)
	{
		if (word == "render") {
			return eRender;
		}
		else if (word == "capture") {
			return eCapture;
		}
		else if (word == "all") {
			return eAll;
		}
		else {
			throw wascap::bad_arguments(wascap::util::string_format("Unrecognized WAS data flow: %s", word));
		}
	}

	ERole parse_role(const std::string& word)
	{
		if (word == "console") {
			return eConsole;
		}
		else if (word == "multimedia") {
			return eMultimedia;
		}
		else if (word == "communications") {
			return eCommunications;
		}
		else {
			throw wascap::bad_arguments(wascap::util::string_format("Unrecognized WAS role: %s", word));
		}
	}

	DWORD parse_state_mask(const std::string& word)
	{
		if (word == "active") {
			return DEVICE_STATE_ACTIVE;
		}
		else if (word == "disabled") {
			return DEVICE_STATE_DISABLED;
		}
		else if (word == "not-present") {
			return DEVICE_STATE_NOTPRESENT;
		}
		else if (word == "unplugged") {
			return DEVICE_STATE_UNPLUGGED;
		}
		else if (word == "all") {
			return DEVICE_STATEMASK_ALL;
		}
		else {
			throw wascap::bad_arguments(wascap::util::string_format("Unrecognized WAS state flag: %s", word));
		}
	}

	void parse_list_arguments(wascap::command_line_arguments& arguments, std::vector<std::string>::const_iterator& current, std::vector<std::string>::const_iterator end)
	{
		if (current != end) {
			arguments.list_flow = parse_flow(*current++);
		}

		for (; current != end; ++current) {
			DWORD state_mask = parse_state_mask(*current);
			parse_assert((arguments.list_state_mask & state_mask) == 0, "Duplicate state flag");
			arguments.list_state_mask |= state_mask;
		}

		if (0 == arguments.list_state_mask) {
			arguments.list_state_mask = DEVICE_STATEMASK_ALL;
		}
	}

	void parse_capture_arguments(wascap::command_line_arguments& arguments, std::vector<std::string>::const_iterator& current, std::vector<std::string>::const_iterator end)
	{
		bool explicit_source = false;

		for (; current != end; ++current) {
			const std::string& word = *current;
			if (word == "shm") {
				parse_assert(arguments.shm_name.empty(), "Duplicate shared memory specification");
				parse_assert(++current != end, "Expected shared memory name");
				arguments.shm_name = *current;
			}
			else if (word == "bind") {
				parse_assert(arguments.bind_address.empty(), "Duplicate bind address specification");
				parse_assert(++current != end, "Expected bind address");
				arguments.bind_address = *current;
			}
			else if (word == "to-was-dev") {
				parse_assert(!arguments.with_was_sink, "Duplicate WAS sink specification");
				arguments.with_was_sink = true;
				parse_assert(++current != end, "Expected WAS sink device ID");
				arguments.sink_device = *current;
			}
			else if (word == "to-was") {
				parse_assert(!arguments.with_was_sink, "Duplicate WAS sink specification");
				arguments.with_was_sink = true;
				parse_assert(++current != end, "Expected WAS sink role");
				arguments.sink_role = parse_role(*current);
			}
			else if (word == "from-was-dev") {
				parse_assert(!explicit_source, "Duplicate source specification");
				explicit_source = true;
				parse_assert(++current != end, "Expected WAS source device ID");
				arguments.source_device = *current;
			}
			else if (word == "from-was") {
				parse_assert(!explicit_source, "Duplicate source specification");
				explicit_source = true;
				parse_assert(++current != end, "Expected WAS source data flow");
				arguments.source_flow = parse_flow(*current);
				parse_assert(++current != end, "Expected WAS source role");
				arguments.source_role = parse_role(*current);
			}
			else if (word == "samplerate") {
				parse_assert(arguments.samplerate == SIZE_MAX, "Duplicate sample rate specification");
				parse_assert(++current != end, "Expected sample rate");
				arguments.samplerate = std::stoi(*current);
			}
			else if (word == "channels") {
				parse_assert(arguments.channel_mask == 0, "Duplicate channel specification");
				parse_assert(++current != end, "Expected channel count");
				int channels = std::stoi(*current);
				parse_assert(0 < channels, wascap::util::string_format("Too few channels: %d", channels));
				parse_assert(channels <= wascap::sink::MAX_CHANNELS, wascap::util::string_format("Too many channels: %d", channels));
				arguments.channel_mask = (channels == wascap::sink::MAX_CHANNELS) ? -1 : ((1U << channels) - 1);
			}
			else if (word == "channel-mask") {
				parse_assert(arguments.channel_mask == 0, "Duplicate channel specification");
				parse_assert(++current != end, "Expected channel mask");
				arguments.channel_mask = std::stoi(*current);
				int channels = __popcnt(arguments.channel_mask);
				parse_assert(0 < channels, wascap::util::string_format("Too few channels: %d", channels));
				parse_assert(channels <= wascap::sink::MAX_CHANNELS, wascap::util::string_format("Too many channels: %d", channels));
			}
			else if (word == "to-network") {
				parse_assert(!arguments.with_network_sink, "Duplicate network sink specification");
				arguments.with_network_sink = true;
			}
			else if (word == "to-network-peer") {
				parse_assert(!arguments.with_network_sink, "Duplicate network sink specification");
				arguments.with_network_sink = true;
				parse_assert(++current != end, "Expected peer address");
				arguments.peer_address = *current;
				parse_assert(++current != end, "Expected peer service");
				arguments.peer_service = *current;
			}
			else if (word == "to-stdout") {
				parse_assert(!arguments.with_stdout_sink, "Duplicate standard output sink specification");
				arguments.with_stdout_sink = true;
			}
			else if (word == "no-shm-tap") {
				parse_assert(arguments.with_shm_tap_sink, "Duplicate shared memory tap specification");
				arguments.with_shm_tap_sink = false;
			}
			else if (word == "no-shm-averaging") {
				parse_assert(arguments.with_shm_averaging_sink, "Duplicate shared memory averaging specification");
				arguments.with_shm_averaging_sink = false;
			}
			else if (word == "duration") {
				parse_assert(arguments.duration == INFINITY, "Duplicate loop specification");
				parse_assert(++current != end, "Expected duration");
				arguments.duration = std::stof(*current);
			}
			else if (word == "lifetime") {
				parse_assert(arguments.lifetime_process == nullptr, "Duplicate lifetime process handle specification");
				parse_assert(++current != end, "Expected process handle");
				static_assert(sizeof(HANDLE) == 8 || sizeof(HANDLE) == 4);
				if constexpr (sizeof(HANDLE) == 8) {
					arguments.lifetime_process = (HANDLE)std::stoll(*current);
				}
				else if constexpr (sizeof(HANDLE) == 4) {
					arguments.lifetime_process = (HANDLE)std::stoi(*current);
				}
			}
			else {
				throw wascap::bad_arguments(wascap::util::string_format("Unrecognized option: %s", word));
			}
		}
	}
}

void wascap::parse_arguments(command_line_arguments& arguments, const std::vector<std::string>& args)
{
	std::vector<std::string>::const_iterator current = args.cbegin();
	std::vector<std::string>::const_iterator end = args.cend();

	parse_assert(current != end, "Expected executable path");
	arguments.executable = *current++;

	if (current != end && *current == "msgbox") {
		arguments.use_message_box = true;
		++current;
	}

	parse_assert(current != end, "Expected verb");
	arguments.verb = parse_verb(*current++);

	switch (arguments.verb) {
	case help:
		// help takes no arguments
		break;
	case list:
		parse_list_arguments(arguments, current, end);
		break;
	case capture:
		parse_capture_arguments(arguments, current, end);
		break;
	default:
		throw wascap::bad_arguments("Verb not implemented (in argument parser)");
	}

	parse_assert(current == end, "Extra arguments found");
}

int wascap::help_main(const command_line_arguments& arguments, const std::exception* exception)
{
	std::ostringstream message;
	if (nullptr != exception) {
		message << "Error while parsing command line:\n" << exception->what();
	}

	std::string msg = message.str();
	if (!msg.empty()) {
		if (arguments.use_message_box) {
			MessageBoxA(nullptr, msg.c_str(), "WASCap", (nullptr != exception) ? MB_ICONERROR : MB_ICONINFORMATION);
		}
		else {
			fprintf(stderr, "%s\n", msg.c_str());;
		}
	}

	return (nullptr != exception) ? 2 : 0;
}