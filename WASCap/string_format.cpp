#include "stdafx.h"

#include "string_format.h"

std::string wascap::util::string_from_wstr(const wchar_t* wstr)
{
	return string_format("%S", wstr);
}

std::unique_ptr<wchar_t[]> wascap::util::wstr_from_string(const char* str)
{
	return wstr_format(L"%S", str);
}

std::unique_ptr<wchar_t[]> wascap::util::wstr_from_string(const std::string& str)
{
	return wstr_format(L"%S", str.c_str());
}