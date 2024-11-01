using UnityEngine;
using System.Runtime.InteropServices;

namespace UnityExposed.Bindings
{
	[StructLayout(LayoutKind.Sequential)]
	public readonly ref struct ManagedSpanWrapper
	{
		public readonly unsafe void* begin;
		public readonly int length;

		public unsafe ManagedSpanWrapper(void* begin, int length)
		{
			this.begin = begin;
			this.length = length;
		}
	}
}
