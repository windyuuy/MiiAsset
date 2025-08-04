using UnityEngine;
using UnityEditor;
using System;

public class AudioOptimizer : AssetPostprocessor
{
	void OnPostprocessAudio(AudioClip audioClip)
	{
		AudioImporter importer = assetImporter as AudioImporter;
		if (importer == null)
		{
			return;
		}

		try
		{
			if (assetPath.StartsWith("Assets/"))
			{
				if (audioClip.loadState != AudioDataLoadState.Loaded)
				{
					Debug.LogError($"cannot load audiodata: {assetPath}, state: {audioClip.loadState}, channels: {audioClip.channels}");
				}

				if (audioClip.channels <= 1)
				{
					importer.forceToMono = true;
				}
				// importer.preloadAudioData = false;
				importer.ambisonic = false;
				var defaultSampleSettings = importer.defaultSampleSettings;
				defaultSampleSettings.preloadAudioData = false;
				if (audioClip.frequency > 44100)
				{
					defaultSampleSettings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
					defaultSampleSettings.sampleRateOverride = 44100;
				}
				else
				{
					defaultSampleSettings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
				}

				if (audioClip.length >= 5)
				{
					defaultSampleSettings.loadType = AudioClipLoadType.Streaming;
					defaultSampleSettings.compressionFormat = AudioCompressionFormat.Vorbis;
					defaultSampleSettings.quality = 0.7f;

					importer.loadInBackground = true;
				}
				else if (audioClip.length >= 1)
				{
					defaultSampleSettings.loadType = AudioClipLoadType.CompressedInMemory;
					defaultSampleSettings.compressionFormat = AudioCompressionFormat.Vorbis;
					defaultSampleSettings.quality = 0.7f;
				}
				else
				{
					defaultSampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
					defaultSampleSettings.compressionFormat = AudioCompressionFormat.ADPCM;
				}

#if UNITY_IOS
			var iosSampleSettings = defaultSampleSettings;
			if(iosSampleSettings.compressionFormat== AudioCompressionFormat.Vorbis)
			{
				iosSampleSettings.compressionFormat = AudioCompressionFormat.MP3;
				iosSampleSettings.quality = 0.6f;
			}
			importer.SetOverrideSampleSettings("iOS", iosSampleSettings);
#endif

				importer.defaultSampleSettings = defaultSampleSettings;

			}
		}catch(Exception e)
		{
			Debug.LogError($"Optimize Audio Failed: {assetPath}");
			Debug.LogException(e);
		}
	}
}
