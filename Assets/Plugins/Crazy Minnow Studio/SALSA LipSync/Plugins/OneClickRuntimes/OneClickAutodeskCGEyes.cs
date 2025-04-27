using UnityEngine;

namespace CrazyMinnow.SALSA.OneClicks
{
	public class OneClickAutodeskCgEyes : MonoBehaviour
	{
		// public static void Setup(GameObject go, int lod = -1)
		public static void Setup(GameObject go, string body)
		{
			string head = "head";
			string eyeL = "lefteye";
			string eyeR = "righteye";
			string blinkL = "^.*reyeclose.*$";
			string blinkR = "^.*leyeclose.*$";

			if (go)
			{
				Eyes eyes = go.GetComponent<Eyes>();
				if (eyes == null)
				{
					eyes = go.AddComponent<Eyes>();
				}
				else
				{
					DestroyImmediate(eyes);
					eyes = go.AddComponent<Eyes>();
				}

				// System Properties
                eyes.characterRoot = go.transform;

                // Heads - Bone_Rotation
                eyes.BuildHeadTemplate(Eyes.HeadTemplates.Bone_Rotation_XY);
                eyes.heads[0].expData.controllerVars[0].bone = Eyes.FindTransform(eyes.characterRoot, head);
                eyes.heads[0].expData.name = "head";
                eyes.heads[0].expData.components[0].name = "head";
                eyes.headTargetOffset.y = 0.052f;
                eyes.CaptureMin(ref eyes.heads);
                eyes.CaptureMax(ref eyes.heads);

                // Eyes - Bone_Rotation
                eyes.BuildEyeTemplate(Eyes.EyeTemplates.Bone_Rotation);
                eyes.eyes[0].expData.controllerVars[0].bone = Eyes.FindTransform(eyes.characterRoot, eyeL);
                eyes.eyes[0].expData.name = "eyeL";
                eyes.eyes[0].expData.components[0].name = "eyeL";
                eyes.eyes[1].expData.controllerVars[0].bone = Eyes.FindTransform(eyes.characterRoot, eyeR);
                eyes.eyes[1].expData.name = "eyeR";
                eyes.eyes[1].expData.components[0].name = "eyeR";
                eyes.CaptureMin(ref eyes.eyes);
                eyes.CaptureMax(ref eyes.eyes);

                // Eyelids - Bone_Rotation
                eyes.BuildEyelidTemplate(Eyes.EyelidTemplates.BlendShapes, Eyes.EyelidSelection.Upper); // includes left/right eyelid
                float blinkMax = 1f;
                // Left eyelid
                eyes.blinklids[0].expData.controllerVars[0].smr = Eyes.FindTransform(eyes.characterRoot, body).GetComponent<SkinnedMeshRenderer>();
				eyes.blinklids[0].expData.controllerVars[0].blendIndex = Eyes.FindBlendIndex(eyes.blinklids[0].expData.controllerVars[0].smr, blinkL);
                eyes.blinklids[0].expData.controllerVars[0].maxShape = blinkMax;
                eyes.blinklids[0].expData.name = "eyelidL";
                // Right eyelid
                eyes.blinklids[1].expData.controllerVars[0].smr = eyes.blinklids[0].expData.controllerVars[0].smr;
                eyes.blinklids[1].expData.controllerVars[0].blendIndex = Eyes.FindBlendIndex(eyes.blinklids[1].expData.controllerVars[0].smr, blinkR);
                eyes.blinklids[1].expData.controllerVars[0].maxShape = blinkMax;
                eyes.blinklids[1].expData.name = "eyelidR";

                // Tracklids
                eyes.CopyBlinkToTrack();
                // Set track eye index
                eyes.tracklids[0].referenceIdx = 0; // left eye
                eyes.tracklids[1].referenceIdx = 1; // right eye

                // Initialize the Eyes module
                eyes.Initialize();
			}
		}
	}
}