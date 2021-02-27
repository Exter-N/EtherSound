#pragma once

namespace wascap
{
	namespace util
	{
		class no_copy
		{
			no_copy(const no_copy&) = delete;

			no_copy& operator =(const no_copy&) = delete;

		protected:
			no_copy() = default;
			~no_copy() = default;
		};

		class no_move
		{
			no_move(const no_move&) = delete;
			no_move(no_move&&) = delete;

			no_move& operator =(const no_move&) = delete;
			no_move& operator =(no_move&&) = delete;

		protected:
			no_move() = default;
			~no_move() = default;
		};

		class no_copy_no_move : public no_copy, public no_move
		{
			no_copy_no_move(const no_copy_no_move&) = delete;
			no_copy_no_move(no_copy_no_move&&) = delete;

			no_copy_no_move& operator =(const no_copy_no_move&) = delete;
			no_copy_no_move& operator =(no_copy_no_move&&) = delete;

		protected:
			no_copy_no_move() = default;
			~no_copy_no_move() = default;
		};
	}
}