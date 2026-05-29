#if UNITY_EDITOR
	using System.Collections.Generic;
	using System.IO;
	using UnityEngine;

	namespace MiiAsset.Editor.Build
	{
		public class AssetBundleTester
		{
			[UnityEditor.MenuItem("Tools/MiiAsset/测试AssetBundles")]
			public static void TestLoadAssetBundle()
			{
				Debug.Log("开始测试AssetBundles");
				var bundles = new string[]
				{
					// "battle_configs_2f9e882c93984ecfbac907cd2747b95f8b16aa19d2c58b1b91f5d478e0d69f94579a9738.bundle",
					// "battle_effectres_a904a16c914b9c3a917db640a9d8c81aaa5ba702fb2dfa82ce2c36b5cdcc30dd265fce9e.bundle",
					// "battle_effectres_tx_lib_edbeadb954f36884a97cf5ac4134e26e43c1a1cbb170610200b9563a0b7c0be8c75a3bfa.bundle",
					// "battle_inputs_785792c69174271fc6adb0720e57366afef59f0501b7a87524bebc6980ae10d357ddc933.bundle",
					// "battle_ui_53fadee2bb50e4fbc9172f3d00bed701195416105fcdb286ade45cfb80558bb2b93c9a4c.bundle",
					// "battle_底部区域_17a6c0f77b9ae32f2a5419f8adeacc004c594770fe2971fa1bdf0d794f98def0c2057795.bundle",
					// "battle_状态栏_e6ac1888cbedecf35f3d2cda568293657ec583a7af4578b00c306c1b94909e8210cec85d.bundle",
					// "battle_装备面板_3b66f0ad8d3a108998aa958a5dfdb732fc5845bb2941ded4aae733d5f7e49fc648047cb4.bundle",
					// "battle_详情界面_d6a8e360d6a7c78cd43ca27e1e851d00d425fc05dd3ed799c71fa6cf6ed18428e808c6b4.bundle",
					// "battleview_chapters_066d43e2a83b9353adeead565139776417c73373a3d7d8bcdea9ed58e5ef650e1dae936b.bundle",
					// "battleview_commonres_17b18c7f2707357b00c63b6ca510cc7585d85f00f0ddb11f79748a1e78055e9ecd0b723f.bundle",
					// "battleview_equips_82802226db2c7f86fbb1b0276df5c38e1f1ceb864432adea515b1d1278b8e06ac2233f22.bundle",
					// "battleview_heros_0eab72e24386a392e9504c0759800f2c6eb9932d4d11f533db8a9702ae4cfcfcd8d32477.bundle",
					// "battleview_levelbg_dc054d9f6b3ac1155bab9d2b8f5784a84e46768e951ec65280a5fa54ec47d829edf7484a.bundle",
					// "battleview_models_d3bf7efc76b567a7cbab0ba9a5e95ea70a902491700d88a67ac0562db88d60974d73bc9a.bundle",
					// "battleview_monsters_e70cd9d954a96265832b9931c04a0211f27e5c70a82eb575e5cf3a7c113ce033fc20db42.bundle",
					// "battleview_ophintviews_c1b854e47c695995c383cee3258583cedc7824f8ec14e3ea2dfdec70f26afdb798b82045.bundle",
					// "battleview_宝物_d141cfa08b7043dbab98883a7a991364dc07785533ed40392fb0953b63fa9771079a6372.bundle",
					// "battleview_机缘_4d3b474ee2c2a434e913371d3d5dcd4a4123ca3fa9641b02b8792efa28dbda80064b5a36.bundle",
					// "battleview_法宝_9be9272d0e79254b1feee53d537f4fee8d69ff087d9fa8aa05dc5895ec41ffbe303cb7ea.bundle",
					// "builtinshader_4bad64ac7b66fa96b223bdf1253f959364fac733d89737d7b08aa950f651c72d7a1c66da.bundle",
					// "effectres_attack_yuancheng_cc02c91ef491c76afb822d0caf7eddf5c8fe148cadf61e77f46d54c60207e7d074cec1e3.bundle",
					// "effectres_baize_674c47f9bc16a6fa2d6ab874412e17f833c9add77fec56586553701de72eeb4cb3516230.bundle",
					// "effectres_buff_chufa_01_9f55c8fc949d59f5ee696acae7eca88b74ab442f3cb6c8dbdab9ccf40f586abb50363756.bundle",
					// "effectres_buff_treat_b7aad211e939292b83cc351b0572149536dba6ca16e72dbb6c5d9c44e9cf428cbf377f70.bundle",
					// "effectres_change_b5774d1611baa02b8b7adaf446cc0a68cdb942aefc674bd5482502078fe2edc9daf64231.bundle",
					// "effectres_common_skill_2db416a685dd4b724b89ad9cb7729cdc8d35c8196f479a083d048d1668c37a7f90d400c0.bundle",
					// "effectres_fonuhuolian_3d018b970dd50eca10c591c3cbd99e0da9037c5beabf19868991d8ab8cdec248a760881e.bundle",
					// "effectres_full_shuixuanwo_477f31c1b5d3fbfe7d4007a50c8dd6e8120d3a7ebf7bf169412c82846e23e6f860245bdf.bundle",
					// "effectres_guili_e9ed7e4253155c01cd3d8f1e0adfa05c32d874ac36da8868645a73e08f535c53ccbcdaec.bundle",
					// "effectres_heiseqixuan_6d742fddf6e9f696a565bb11e1c2cf346952bdc1e2238de3adc0c8d25a30aac89dc86188.bundle",
					// "effectres_huxianfeng_06b815268e2c9a55073c2504080b5827ddf9c69cd938a38ef53b53681d2c00b85b1db101.bundle",
					// "effectres_nezha_543a4954e54303fa3004278e6c5e3024f2c5486ccaa80e684e3c142c0a87a47cbd9bbf68.bundle",
					// "effectres_niumo_b42469f07e2db636768b48f76f9e1ef45ce0f6b6f74137e02e5a82c1e14ae93e633645c4.bundle",
					// "effectres_sanjianliangren_84014965fddc564a8ea0d87479c360eb2ac2224ae8e193a25d5bbfd1b3f5e8f0a2a1496e.bundle",
					// "effectres_sanjiban_0053f18f6d813f832a756f6b223ccf8296865bf74b5d169382cf4387f6556f361b5159f8.bundle",
					// "effectres_sanlianjianqi_f3ec5f27668027f48d572698082fc5e835fbf7ca4a7667d0b2de882215020854b19c7e7f.bundle",
					// "effectres_tianshengshenmu_44f0ca03a31ad0eefa141539250d2ab962c319e64d7e73300cdc21edaacc793561936d1d.bundle",
					// "effectres_wd_spirit_hit_13f7f56ee4fbafcd1358a5af7101e2249c2602e058f88a6c4bb1e6eca02b464fdfa3458a.bundle",
					// "effectres_yehei_7537b5818e93616f053dc4c8cb36379345ac9d42777b77e63673929ee8eb1a760f7fc67b.bundle",
					// "effectres_yihuo_b12bc42f6ea29dd9d8ed8739360edcb8e47bcf66fd77eed01f5adce255d252733d10ac69.bundle",
					// "fonts_bmfs_6c1490f89e22ce19ade0b33e92bbd0ca78893d7d4849bd6feb1d56748366f96636309fc2.bundle",
					// "fonts_hyshangweishoushuw.ttf_3e05e5c4414b830671deaff5330d136dcabbdb34557787ed49de599dba056066203ba9aa.bundle",
					// "fonts_lxgwwenkailight.ttf_bf907149f8034359047e67c56d19877896e341e602d977bbf6d9bad89642fbbf15472c25.bundle",
					// "gameconfigs_battleconfig_ee686d2aea510dc2a32db7a0a9790b7867e8bcef956a03347a9cf923d3cd35269960dddd.bundle",
					// "uires_b7cdb5a56ac26b2605355d197c63b626b8bf78401f2b27aeb237d405098dbc4459f80eb3.bundle",
					// "uisysdefaults_6a8d52dc41a40a9f4ad7e5bb19cafd0c327a0a978cea37a145cae60fba039ed519a16ea0.bundle",
				};
				//
				// var mainBundleFile = "ui机缘卡界面_30fc5861843765adba546906a122f46740ab9c298dffe535b2815697ef634b7e245a234e.bundle";
				// foreach (var bundle in bundles)
				// {
				// 	var uri = $"AssetBundles/Android/{bundle}";
				// 	var bytes = File.ReadAllBytes(uri);
				// 	var assetBundle = AssetBundle.LoadFromMemory(bytes);
				// 	if (assetBundle == null)
				// 	{
				// 		Debug.LogError($"bundleisnull: {uri}");
				// 	}
				// }

				var dir = "AssetBundles/Android/";
				var files = Directory.GetFiles(dir);
				var ls = new List<AssetBundle>();
				foreach (var file in files)
				{
					if (file.EndsWith("_hatill") || file.EndsWith("_prehatill"))
					{
						Debug.Log($"test bundle: {file}");
						var bytes0 = File.ReadAllBytes(file);
						var mainBundle = AssetBundle.LoadFromMemory(bytes0);
						// var asset = mainBundle.LoadAsset<GameObject>("@机缘卡界面:机缘选择UI.prefab");
						// var obj = GameObject.Instantiate(asset);
						if (!mainBundle.name.EndsWith("_scene"))
						{
							var assets = mainBundle.LoadAllAssets();
						}

						ls.Add(mainBundle);
					}
				}

				foreach (var assetBundle in ls)
				{
					assetBundle.Unload(true);
				}

				Debug.Log("结束测试AssetBundles");
			}
		}
	}
#endif