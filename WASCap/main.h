#pragma once

#include <Windows.h>
#include <mmdeviceapi.h>
#include <stdexcept>
#include <string>
#include <vector>

namespace wascap
{
	class bad_arguments : public std::runtime_error
	{
	public:
		inline bad_arguments(const std::string& what) : std::runtime_error(what) { }
	};

	enum verb
	{
		help,
		list,
		capture,
	};

	struct command_line_arguments
	{
		std::string executable = "";

		verb verb = help;

		std::string shm_name = "";
		std::string bind_address = "";
		std::string peer_address = "";
		std::string peer_service = "";
		std::string sink_device = "";
		std::string source_device = "";

		size_t samplerate = SIZE_MAX;
		DWORD channel_mask = 0;

		ERole sink_role = eConsole;

		EDataFlow source_flow = eRender;
		ERole source_role = eConsole;

		EDataFlow list_flow = eAll;
		DWORD list_state_mask = 0;

		bool with_network_sink = false;
		bool with_stdout_sink = false;
		bool with_was_sink = false;
		bool with_shm_tap_sink = true;
		bool with_shm_averaging_sink = true;

		float duration = INFINITY;
		bool use_message_box = false;
	};

	void parse_arguments(wascap::command_line_arguments& arguments, const std::vector<std::string>& args);

	int help_main(const wascap::command_line_arguments& arguments, const std::exception* exception);
	int list_main(const wascap::command_line_arguments& arguments);
	int capture_main(const wascap::command_line_arguments& arguments);
}