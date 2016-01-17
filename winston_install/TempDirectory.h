#pragma once
#include "stdafx.h"

class TempDirectory
{
	std::wstring path;

	static int removeDirectory(const std::wstring& dir)
	{
		std::vector<wchar_t> buf(dir.begin(), dir.end());
		// Double-null termination is expected by this API
		buf.push_back(0);
		buf.push_back(0);

		SHFILEOPSTRUCT file_op = {
			nullptr,
			FO_DELETE,
			buf.data(),
			L"",
			FOF_NOCONFIRMATION |
			FOF_NOERRORUI |
			FOF_SILENT,
			false,
			0,
			L"" };
		int ret = SHFileOperation(&file_op);
		return ret; // returns 0 on success, non zero on failure.
	}

public:
	TempDirectory(const std::wstring path)
	{
		this->path = path;
		CreateDirectory(path.c_str(), nullptr);
	}

	std::wstring Path() const
	{
		return this->path;
	}

	LPCWSTR PathCStr() const
	{
		return this->path.c_str();
	}

	~TempDirectory()
	{
		removeDirectory(path);
	}
};