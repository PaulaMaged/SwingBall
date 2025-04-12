using UnityEditor;
using UnityEngine;

namespace CrazyMinnow.SALSA.OneClicks
{
	/// <summary>
	/// RELEASE NOTES:
	///		2.7.2 (2023-09-28):
	///			REQUIRES: OneClickBase and Base core files v2.7.2+, does NOT work with prior versions.
	///			REMOVED prefab breakdown dependency.
	/// 	2.5.0 (2020-08-20):
	/// 		REQUIRES: SALSA LipSync Suite v2.5.0+, does NOT work with prior versions.
	/// 		REQUIRES: OneClickBase v2.5.0+
	/// 		+ Support for Eyes module v2.5.0+
	/// 		+ LOD Manager and normal OneClick options.
	/// 		+ LOD Manager can now set a default AudioClip.
	/// 		+ Adds Advanced Dynamics Silence Analyzer to character.
	/// 		+ Leverages new QueueProcessor feature to shutdown existing components in specific queues --
	/// 			helps avoid orphaned components from previous LOD activations while playing.
	/// 		~ Several methods moved to public to allow for more flexible API-like usage.
	/// 		~ Tweaks to SALSA settings.
	///		2.3.0 (2020-02-02):
	/// 		~ updated to operate with SALSA Suite v2.3.0+
	/// 		NOTE: Does not work with prior versions of SALSA Suite (before v2.3.0)
	/// 	2.1.2 (2019-07-03):
	/// 		- confirmed operation with Base 2.1.2
	///			+ Initial release.
	/// ==========================================================================
	/// PURPOSE: This script provides simple, simulated lip-sync input to the
	///		Salsa component from text/string values. For the latest information
	///		visit crazyminnowstudio.com.
	/// ==========================================================================
	/// DISCLAIMER: While every attempt has been made to ensure the safe content
	///		and operation of these files, they are provided as-is, without
	///		warranty or guarantee of any kind. By downloading and using these
	///		files you are accepting any and all risks associated and release
	///		Crazy Minnow Studio, LLC of any and all liability.
	/// ==========================================================================
	/// </summary>
	public class OneClickAutodeskCGEditor : Editor
	{
		[MenuItem("GameObject/Crazy Minnow Studio/SALSA LipSync/One-Clicks/AutodeskCG LOD Manager")]
		public static void AutodeskCGLODSetup()
		{
			GameObject go = Selection.activeGameObject;

			var lodMgr = go.GetComponent<OneClickAutodeskCGLod>();
			if (lodMgr == null)
				lodMgr = go.AddComponent<OneClickAutodeskCGLod>();

			if (lodMgr == null)
				Debug.Log("Unable to add an AutodeskCD LOD Manager component.");
			else
				lodMgr.audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(OneClickBase.RESOURCE_CLIP);
		}


		[MenuItem("GameObject/Crazy Minnow Studio/SALSA LipSync/One-Clicks/AutodeskCG")]
		public static void AutodeskCGSetup()
		{
			GameObject go = Selection.activeGameObject;

			ApplyOneClick(go);
		}

		private static void ApplyOneClick(GameObject go)
		{
			// Use the OneClickAutodesk LOD manager to locate the max lod for OneClick configuration.
			var lodMgr = go.GetComponent<OneClickAutodeskCGLod>();
			if (lodMgr == null)
				lodMgr = go.AddComponent<OneClickAutodeskCGLod>();

			if (lodMgr == null)
			{
				Debug.Log("Cannot find/add OneClickAutodesk LOD manager to object: " + go.name);
				return;
			}

			// find the max lod and configure it...
			var lodLevel = lodMgr.MaxLod;
			lodMgr.DeactivateAllLODs();
			lodMgr.SetLOD(lodLevel);
			const int BODY = 0;
			
			// setup SALSA Suite
			OneClickAutodeskCG.Setup(go, lodMgr.keySmrs[lodLevel]);
			OneClickAutodeskCgEyes.Setup(go, lodMgr.keySmrs[lodLevel][BODY].name);
			DestroyImmediate(lodMgr); // remove the lodMgr...
			
			// add QueueProcessor
			OneClickBase.AddQueueProcessor(go);

			// configure AudioSource
			var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(OneClickBase.RESOURCE_CLIP);
			OneClickBase.ConfigureSalsaAudioSource(go, clip, true);
		}
	}
}