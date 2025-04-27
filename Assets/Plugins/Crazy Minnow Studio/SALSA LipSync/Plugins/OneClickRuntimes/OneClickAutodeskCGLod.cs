using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CrazyMinnow.SALSA.OneClicks
{
	public class OneClickAutodeskCGLod : MonoBehaviour
	{
		public Salsa salsa;
		public AudioClip audioClip;
		public Emoter emoter;
		public Eyes eyes;

		private List<string> lodSmrSearches =
			new List<string>(4)
			{
				"^.*((c_[^(dds)])|(h_dds_crowd)|(lod0)).*$", // lod0
				"^.*((l_[^(dds)])|(h_dds_low)|(lod1)).*$",	// lod1
				"^.*((m_[^(dds)])|(h_dds_mid)|(lod2)).*$",	// lod2
				"^.*((h_[^(dds)])|(h_dds_high)|(lod3)).*$"	// lod3
			};

		// lodSmrs used for activating and deactivating LODs.
		private List<List<SkinnedMeshRenderer>> lodSmrSets = new List<List<SkinnedMeshRenderer>>();
		// keySmrs used for switching SALSA/EmoteR components.
		public List<List<SkinnedMeshRenderer>> keySmrs = new List<List<SkinnedMeshRenderer>>();
		public const int BODY = 0;
		public const int TEETH = 1;
		[SerializeField] private int activeLOD = -1;
		private int maxLOD = -1; // max valid LOD available.
		private bool hasSearchedSMRs = false;
		private bool hasSuiteReferences = false;

		public int MaxLod
		{
			get
			{
				if (!hasSearchedSMRs)
					FindAndLoadSMRs();
				return maxLOD;
			}
		}

		public int ActiveLod
		{
			get { return activeLOD; }
		}

		private bool IsInValidLodRange(int lod)
		{
			// does our smr list have smr's?
			return lod >= 0 && lod <= MaxLod;
		}

		private void Awake()
		{
			// This can perform a OneClick config at runtime...if the object is not already configured.
			OneClickConfig();
		}

		public void OneClickConfig()
		{
			// Find and deactivate all LODs associated with the configured SMR object searches.
			if (!hasSearchedSMRs)
				FindAndLoadSMRs();
			DeactivateAllLODs(); // fresh slate

			// confirm specified lod is valid...or set max lod.
			activeLOD = IsInValidLodRange(ActiveLod) ? ActiveLod : MaxLod;

			// confirm lod is successfully set and then update SALSA Suite.
			if (!SetLOD(ActiveLod))
				Debug.Log("Something went wrong, unable to set the LOD to " + ActiveLod);
		}

		/// <summary>
		/// Search for and store SMRs in appropriate LOD groups and set the MaxLOD available.
		/// </summary>
		public void FindAndLoadSMRs()
		{
			// ensure the list of lod/key SMRs is clean.
			lodSmrSets.Clear();
			keySmrs.Clear();

			// get smrs for each lod level
			for (int i = 0; i < lodSmrSearches.Count; i++)
			{
				lodSmrSets.Add(new List<SkinnedMeshRenderer>());
				keySmrs.Add(new List<SkinnedMeshRenderer>());
				// since we need to keep track of the SMRs in specific order (for eyes), not order found,
				// add two placeholders so we can update the correct one with the correct smr.
				keySmrs[i].Add(new SkinnedMeshRenderer());
				keySmrs[i].Add(new SkinnedMeshRenderer());

				foreach (Transform child in transform)
				{
					if (Regex.IsMatch(child.name, lodSmrSearches[i], RegexOptions.IgnoreCase))
					{
						var possibleSmr = child.GetComponent<SkinnedMeshRenderer>();
						if (possibleSmr != null)
						{
							lodSmrSets[i].Add(possibleSmr);

							// save reference to key smr components for switching Salsa/Emoter/Eyes...
							if (child.name.ToLower().Contains("h_dds_"))
								keySmrs[i][BODY] = possibleSmr;
							if (child.name.ToLower().Contains("teethdown"))
								keySmrs[i][TEETH] = possibleSmr;
						}
					}
				}
			}

			PruneLodSets();

			foreach (var lodSmrSet in lodSmrSets)
			{
				foreach (var smr in lodSmrSet)
					Debug.Log("smr = " + smr);
			
				Debug.Log("=================");
			}
			maxLOD = lodSmrSets.Count - 1;
			hasSearchedSMRs = true;
		}

		// Ensures only valid LOD sets are configured.
		private void PruneLodSets()
		{
			for (int i = lodSmrSets.Count - 1; i >= 0; i--)
				if (lodSmrSets[i].Count == 0)
				{
					lodSmrSets.RemoveAt(i);
					keySmrs.RemoveAt(i);
				}
		}

		/// <summary>
		/// Ensure all Suite references are linked. Requires an LOD to be active.
		/// </summary>
		/// <param name="lodLevel"></param>
		/// <param name="clip"></param>
		private bool SetReferences(int lodLevel, AudioClip clip)
		{
			salsa = GetComponent<Salsa>();
			emoter = GetComponent<Emoter>();
			eyes = GetComponent<Eyes>();

			if (salsa == null || emoter == null)
			{
				OneClickAutodeskCG.Setup(gameObject, keySmrs[lodLevel]);
				salsa = GetComponent<Salsa>();
				emoter = GetComponent<Emoter>();

				if (salsa == null || emoter == null)
				{
					Debug.Log("Cannot find SALSA and/or EmoteR modules.");
					return false;
				}
			}

			if (eyes == null)
			{
				OneClickAutodeskCgEyes.Setup(gameObject, keySmrs[lodLevel][BODY].name);
				eyes = GetComponent<Eyes>();

				if (eyes == null)
				{
					Debug.Log("Cannot find the Eyes module.");
					return false;
				}
			}

			hasSuiteReferences = true;
			return true;
		}

		private void ActivateLOD(int lodToActivate)
		{
			foreach (var smr in lodSmrSets[lodToActivate])
				smr.gameObject.SetActive(true);

			activeLOD = lodToActivate; // update the current LOD pointer.
		}

		public void DeactivateLOD(int lodToDeactivate)
		{
			if (IsInValidLodRange(lodToDeactivate))
				foreach (var smr in lodSmrSets[lodToDeactivate])
					smr.gameObject.SetActive(false);
		}

		/// <summary>
		/// Deactivates all LODs for a clean slate. Should only be called on initialization.
		/// </summary>
		public void DeactivateAllLODs()
		{
			foreach (var lod in lodSmrSets)
				foreach (var smr in lod)
					smr.gameObject.SetActive(false);
		}

		/// <summary>
		/// Activate new LOD if within valid LOD list range.
		/// </summary>
		/// <param name="lodLevel"></param>
		public bool SetLOD(int lodLevel)
		{
			if (IsInValidLodRange(lodLevel))
			{
				ActivateLOD(lodLevel);
				UpdateSalsaSuite(lodLevel);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Helper used for UI buttons (etc.) which can't receive a return.
		/// </summary>
		public void IncreaseLOD()
		{
			if (!BumpLOD(+1))
				Debug.Log("Cannot increase LOD.");
		}
		/// <summary>
		/// Helper used for UI buttons (etc.) which can't receive a return.
		/// </summary>
		public void DecreaseLOD()
		{
			if (!BumpLOD(-1))
				Debug.Log("Cannot decrease LOD.");
		}
		/// <summary>
		/// Increase/Decrease the LOD to the next available LOD.
		/// </summary>
		/// <returns>True=success; False=failure (no higher LOD found)</returns>
		public bool BumpLOD(int bumpAmount)
		{
			var prevLod = ActiveLod;
			var newLod = ActiveLod + bumpAmount;

			if (IsInValidLodRange(newLod))
			{
				if (SetLOD(newLod))
				{
					DeactivateLOD(prevLod); // turn off previous LOD
					return true;
				}

				return false;
			}

			return false;
		}

		private void UpdateSalsaSuite(int lodLevel)
		{
			if (!hasSuiteReferences)
				if (!SetReferences(lodLevel, audioClip))
					Debug.Log("Unable to set all references...");

			if (salsa != null)
			{
				// Leverage new QueueProcessor feature to shutdown existing queue components. This removes
				// zombie component artifacts from previous LOD activations which could remain present until
				// the same component is fired on the new LOD.
				if (Application.isPlaying)
				{
					const int SalsaQueue = 4; // simply for clarification
					salsa.queueProcessor.ShutdownActiveQueue(SalsaQueue);
				}

				// flip Salsa smrs to new lod.
				foreach (var viseme in salsa.visemes)
				{
					foreach (var controllerVar in viseme.expData.controllerVars)
					{
						if (controllerVar.smr.name.ToLower().Contains("h_dds_"))
							controllerVar.smr = keySmrs[lodLevel][BODY]; // always body smr
						else
							controllerVar.smr = keySmrs[lodLevel][TEETH]; // always teeth smr
					}
				}

				salsa.UpdateExpressionControllers();
			}

			if (emoter != null)
			{
				// see explanation in SALSA configuration above...
				if (Application.isPlaying)
				{
					const int EmoterQueue = 3; // simply for clarification...
					emoter.queueProcessor.ShutdownActiveQueue(EmoterQueue);
				}

				// flip Emoter smrs to new lod
				foreach (var emote in emoter.emotes)
				{
					foreach (var controllerVar in emote.expData.controllerVars)
						controllerVar.smr = keySmrs[lodLevel][BODY]; // always body smr
				}

				emoter.UpdateExpressionControllers();
				emoter.UpdateEmoteLists();
			}

			if (eyes != null)
			{
				if (eyes.blinklids.Count > 0)
				{
					// flip Eyes (blink) smrs to new lod
					foreach (var blinklid in eyes.blinklids)
					{
						foreach (var controllerVar in blinklid.expData.controllerVars)
							controllerVar.smr = keySmrs[lodLevel][BODY]; // body smr
					}

					eyes.UpdateRuntimeExpressionControllers(ref eyes.blinklids);
				}

				if (eyes.tracklids.Count > 0)
				{
					// flip Eyes (track) smrs to new lod
					foreach (var tracklid in eyes.tracklids)
					{
						foreach (var controllerVar in tracklid.expData.controllerVars)
							controllerVar.smr = keySmrs[lodLevel][0]; // body smr
					}
				}
			}
		}
	}
}