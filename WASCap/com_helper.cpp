#include "stdafx.h"

#include <windows.h>
#include <objbase.h>

#include "com_helper.h"
#include "errors.h"

wascap::util::com::com()
{
	COM_CHECK(CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED));
}

wascap::util::com::~com()
{
	CoUninitialize();
}

wascap::util::shared_com wascap::util::make_shared_com()
{
	class concrete_com : public com { };

	return dropbox::oxygen::nn_make_shared<concrete_com>();
}

wascap::util::com_prop_variant::com_prop_variant()
{
	PropVariantInit(&m_var);
}

wascap::util::com_prop_variant::~com_prop_variant()
{
	PropVariantClear(&m_var);
}