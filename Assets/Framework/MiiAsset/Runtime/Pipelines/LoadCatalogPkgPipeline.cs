using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadCatalogPkgPipeline: ILoadTextAssetPipeline
	{
		protected string CatalogUri;
		public LoadCatalogPkgPipeline Init(string catalogUri)
		{
			this.CatalogUri = catalogUri;
			this.Build();
			return this;
		}
		
		public void Dispose()
		{
			
		}

		public void Build()
		{
			
		}

		public async Task Run()
		{
			var stream = IOManager.LocalIOProto.OpenRead(CatalogUri);
			using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
			var entry = zipArchive.GetEntry("catalog.json");
			Debug.Assert(entry!=null,"entry!=null");
			using var streamReader = new StreamReader(entry.Open());
			var text = await streamReader.ReadToEndAsync();
			Text = text;
		}

		public bool IsCached()
		{
			return false;
		}

		public string Text { get; set; }
	}
}