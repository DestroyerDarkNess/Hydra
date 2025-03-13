using System;

namespace EXGuard.Core.RT {
	public class BinaryChunk : IChunk {
		public BinaryChunk(byte[] data) {
			Data = data;
		}

		public byte[] Data { get; private set; }
		public uint Offset { get; private set; }

		public EventHandler<OffsetComputeEventArgs> OffsetComputed;

		uint IChunk.Length {
			get { return (uint)Data.Length; }
		}

		void IChunk.OnOffsetComputed(uint offset) {
			if (OffsetComputed != null)
				OffsetComputed(this, new OffsetComputeEventArgs(offset));
			Offset = offset;
		}

		byte[] IChunk.GetData() {
			return Data;
		}
	}

	public class OffsetComputeEventArgs : EventArgs {
		internal OffsetComputeEventArgs(uint offset) {
			Offset = offset;
		}

		public uint Offset { get; private set; }
	}
}