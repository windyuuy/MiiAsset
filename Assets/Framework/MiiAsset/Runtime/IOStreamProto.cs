using System;
using System.Threading.Tasks;

namespace Framework.MiiAsset.Runtime
{
	public interface IIOProgress
	{
		public float Progress { get; }
	}

	public interface IInputProto : IIOProgress
	{
		public IPumpStream GetPumpStream(Uri uri);
	}

	public interface IOutputProto : IIOProgress
	{
		public IWriteStream GetWriteStream(Uri uri);
	}

	public enum StreamEvent
	{
		Begin,
		Capability,
		Data,
		End,
	}

	public struct StreamCtrlEvent
	{
		public StreamEvent Event;
		public long Code;
		public string Msg;
		public bool IsOk;
		public int Capability;
		public IPumpStream PumpStream;
	}

	public static class PumpStreamExt
	{
		public static void BindReadStream(this IPumpStream pumpStream, IWriteStream writeStream)
		{
			pumpStream.OnReceivedData += writeStream.Write;
			pumpStream.OnCtrl += writeStream.OnCtrl;
		}

		public static void UnBindReadStream(this IPumpStream pumpStream, IWriteStream writeStream)
		{
			pumpStream.OnReceivedData -= writeStream.Write;
			pumpStream.OnCtrl -= writeStream.OnCtrl;
		}

		public static void BindWriteStream(this IRandomWritePumpStream pumpStream, IRandomReadStream readStream)
		{
			pumpStream.ReadStream = readStream;
		}

		public static void UnBindWriteStream(this IRandomWritePumpStream pumpStream, IRandomReadStream readStream)
		{
			pumpStream.ReadStream = null;
		}
	}

	public interface IPumpStream : IDisposable
	{
		public Task<PipelineResult> Start();
		public void Abort();
		public Func<byte[], int, int, int> OnReceivedData { get; set; }
		public Action<StreamCtrlEvent> OnCtrl { get; set; }
		public PipelineProgress GetProgress();
	}

	public interface IWriteStream : IDisposable
	{
		public int Write(byte[] data, int offset, int len);
		public void OnCtrl(StreamCtrlEvent evt);
		public Task<PipelineResult> WaitDone();
		public PipelineProgress GetProgress();
	}

	public interface IRandomReadStream : ISeekableStream, ICacheableStream
	{
		void Run();
	}

	public interface IRandomWritePumpStream : IDisposable
	{
		public IRandomReadStream ReadStream { get; set; }
	}

	public interface ISeekableStream
	{
		public long GetLength();
		public long GetPosition();
		public long SetPosition(long pos);
	}

	public interface ICacheableStream : IDisposable
	{
		public bool Exist();
		public int Read(byte[] data, int offset, int len);
	}
}