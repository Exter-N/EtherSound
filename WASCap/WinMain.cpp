#include "stdafx.h"

#include <Windows.h>
#include <fcntl.h>
#include <io.h>
#include <shellapi.h>
#include <string>
#include <vector>

#include "main.h"
#include "string_format.h"
#include "win32_helper.h"
#include "errors.h"

int CALLBACK WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	setvbuf(stderr, nullptr, _IONBF, 0);
	setvbuf(stdout, nullptr, _IONBF, 0);
	_setmode(_fileno(stdout), O_BINARY);

	// MessageBoxA(nullptr, string_format("Initializing WASCap (PID %d)", GetCurrentProcessId()).c_str(), "WASCap", MB_ICONINFORMATION);

	std::vector<std::string> args;
	{
		int argc;
		std::unique_ptr<LPWSTR[], wascap::util::local_deleter<LPWSTR[]>> argvW(WIN32_CHECK(CommandLineToArgvW(GetCommandLineW(), &argc)));
		args.reserve(argc);
		for (int i = 0; i < argc; ++i) {
			args.push_back(wascap::util::string_from_wstr(argvW[i]));
		}
	}

	wascap::command_line_arguments arguments;
	try {
		wascap::parse_arguments(arguments, args);
	}
	catch (const std::exception& e) {
		return wascap::help_main(arguments, &e);

		return 2;
	}

	try {
		if (arguments.lifetime_process) {
			CloseHandle(WIN32_CHECK(CreateThread(nullptr, 0, wascap::bind_lifetime, arguments.lifetime_process, 0, nullptr)));
		}

		switch (arguments.verb) {
		case wascap::help:
			return wascap::help_main(arguments, nullptr);
		case wascap::list:
			return wascap::list_main(arguments);
		case wascap::capture:
			return wascap::capture_main(arguments);
		default:
			if (arguments.use_message_box) {
				MessageBoxA(nullptr, "Verb not implemented (in main)", "WASCap", MB_ICONERROR);
			}
			else {
				fprintf(stderr, "Verb not implemented (in main)\n");
			}

			return 2;
		}
	}
	catch (const std::exception& e) {
		if (arguments.use_message_box) {
			MessageBoxA(nullptr, e.what(), "WASCap", MB_ICONERROR);
		}
		else {
			fprintf(stderr, "%s\n", e.what());
		}

		return 1;
	}
}