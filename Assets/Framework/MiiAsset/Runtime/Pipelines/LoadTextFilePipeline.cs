using System.IO;
using System.Text;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using Framework.MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadTextFilePipeline : ILoadTextAssetPipeline
	{
		protected string Uri;

		public LoadTextFilePipeline Init(string uri)
		{
			Uri = uri;
			this.Build();
			return this;
		}

		public void Build()
		{
		}

		public string Text { get; set; }

		public async Task Run()
		{
			if (Text == null)
			{
				Text = await IOManager.LocalIOProto.ReadAllTextAsync(Uri, Encoding.UTF8);
			}
		}

		public bool IsCached()
		{
			return IOManager.LocalIOProto.Exists(Uri);
		}

		public void Dispose()
		{
		}
	}
}