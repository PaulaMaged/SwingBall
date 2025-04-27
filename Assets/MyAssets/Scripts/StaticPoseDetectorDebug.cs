using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using com.rfilkov.kinect;
using System.Linq;
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace com.rfilkov.components
{
    /// <summary>
    /// Static pose detector check whether the user's pose matches a predefined, static model's pose.
    /// </summary>
    public class StaticPoseDetectorDebug : MonoBehaviour
    {
        [Tooltip("User avatar model, who needs to reach the target pose.")]
        public PoseModelHelper avatarModel;

        [Tooltip("Model in pose that need to be reached by the user.")]
        public PoseModelHelper poseModel;

        [Tooltip("List of joints to compare.")]
        private List<KinectInterop.JointType> poseJoints = new();

        [Tooltip("For testing the joint weights applied")]
        public Exercise exercise;

        [Tooltip("Allowed delay in pose match, in seconds. 0 means no delay allowed.")]
        [Range(0f, 10f)]
        public float delayAllowed = 2f;

        [Tooltip("Time between pose-match checks, in seconds. 0 means check each frame.")]
        [Range(0f, 1f)]
        public float timeBetweenChecks = 0.1f;

        [Tooltip("Threshold, above which we consider the pose is matched.")]
        [Range(0.5f, 1f)]
        public float matchThreshold = 0.7f;

        [Tooltip("Duration for slerping in seconds")]
        [Range(0f, 10f)]
        public float slerpDurationSecondsFallback;

        [Tooltip("GUI-Text to display information messages.")]
        public UnityEngine.UI.Text infoText;

        [Tooltip("Key to be used to skip a joint slerp")]
        public Key skipJointSlerpKeyEnum;

        //the list body joint weights
        private Dictionary<KinectInterop.JointType, JointInfo> joint2WeightAndMaxAngles = new();

        // whether the pose is matched or not
        private bool bPoseMatched = false;
        // match percent (between 0 and 1)
        private float fMatchPercent = 0f;

        // initial rotation
        private Quaternion initialAvatarRotation = Quaternion.identity;
        private Quaternion initialPoseRotation = Quaternion.identity;

        // reference to the avatar controller
        private AvatarController avatarController = null;

        // uncomment to get debug info
        private StringBuilder sbDebug = new StringBuilder();

        // data for each saved pose
        private class PoseModelData
        {
            public float fTime;
            public Quaternion[] avBoneOrientations;
        }

        // list of saved pose data
        private List<PoseModelData> alSavedPoses = new List<PoseModelData>();

        // current avatar pose
        private PoseModelData poseAvatar = new PoseModelData();

        private bool IsMirrored;

        //slerping related
        private Coroutine SlerpJointsSequentiallyCoroutine;
        private KeyControl SkipJointSlerpKey;

        [SerializeField] float AccuracySettleTimeSeconds;

        /// <summary>
        /// Determines whether the target pose is matched or not.
        /// </summary>
        /// <returns><c>true</c> if the target pose is matched; otherwise, <c>false</c>.</returns>
        public bool IsPoseMatched()
        {
            return bPoseMatched;
        }


        /// <summary>
        /// Gets the pose match percent.
        /// </summary>
        /// <returns>The match percent (value between 0 and 1).</returns>
        public float GetMatchPercent()
        {
            return fMatchPercent;
        }

        private void Awake()
        {
            if (avatarModel)
            {
                initialAvatarRotation = avatarModel.transform.rotation;
                avatarController = avatarModel.gameObject.GetComponent<AvatarController>();
            }

            if (poseModel)
            {
                initialPoseRotation = poseModel.transform.rotation;
            }
        }

        private void Start()
        {
            SkipJointSlerpKey = Keyboard.current[skipJointSlerpKeyEnum];
            SetJoints();
        }

        void Update()
        {
            // get mirrored state
            IsMirrored = avatarController ? avatarController.mirroredMovement : false;  // false by default

            if (avatarModel != null)
            {
                // get the difference
                GetPoseDifference(IsMirrored);

                if (infoText != null)
                {
                    //string sPoseMessage = string.Format("Pose match: {0:F0}% {1:F1}s ago {2}", fMatchPercent * 100f, Time.realtimeSinceStartup - fMatchPoseTime,
                    //                                    (bPoseMatched ? "- Matched" : ""));
                    string sPoseMessage = string.Format("Pose match: {0:F0}% {1}", fMatchPercent * 100, (fMatchPercent >= matchThreshold ? "- Matched" : ""));
                    if (sbDebug != null)
                        sPoseMessage += sbDebug.ToString();
                    infoText.text = sPoseMessage;
                }
            }
            else
            {
                // no user found
                fMatchPercent = 0f;
                bPoseMatched = false;

                if (infoText != null)
                {
                    infoText.text = "Try to follow the model pose.";
                }
            }
        }

        [ContextMenu("Set Exercise Joints")]
        // Fix for the foreach loop causing multiple errors
        public void SetJoints()
        {
            joint2WeightAndMaxAngles = exercise.Joint2WeightAndMaxAngle;

            poseJoints.Clear();
            foreach (KeyValuePair<KinectInterop.JointType, JointInfo> jointData in joint2WeightAndMaxAngles)
            {
                poseJoints.Add(jointData.Key);
            }
        }

        /// <summary>
        /// Interpolates the avatar's joints to match those of the reference
        /// </summary>
        [ContextMenu("Start Slerping")]
        private void StartSlerping()
        {
            if (avatarController != null) avatarController.enabled = false;
            GetAvatarPose();

            List<(Transform from, Transform to)> avatarJoint2ReferenceJoint = new();
            foreach (var joint in poseJoints)
            {
                Transform avatarJointTransform = avatarModel.GetBoneTransform(PoseModelHelper.GetBoneIndexByJoint(joint, IsMirrored));
                Transform referenceJointTransform = poseModel.GetBoneTransform(PoseModelHelper.GetBoneIndexByJoint(joint, IsMirrored));
                avatarJoint2ReferenceJoint.Add((avatarJointTransform, referenceJointTransform));
            }

            SlerpJointsSequentiallyCoroutine = StartCoroutine(SlerpJointsSequentially(avatarJoint2ReferenceJoint));
        }

        /// <summary>
        /// Stops the slerping of avatar onto reference and resets orientation of joints
        /// </summary>
        [ContextMenu("Reset pose from slerped")]
        private void ResetFromSlerping()
        {
            if (avatarController != null) avatarController.enabled = true;

            if (SlerpJointsSequentiallyCoroutine != null)
            {
                StopCoroutine(SlerpJointsSequentiallyCoroutine);
                SlerpJointsSequentiallyCoroutine = null;
            }

            for (int i = 0; i < poseJoints.Count; i++)
            {
                Transform avatarJointTransform = avatarModel.GetBoneTransform(PoseModelHelper.GetBoneIndexByJoint(poseJoints[i], IsMirrored));
                avatarJointTransform.localRotation = poseAvatar.avBoneOrientations[i];
            }
        }

        /// <summary>
        /// For each joint inside of poseJoints, a slerping process takes place to map the joint to the reference joint
        /// using an coroutine
        /// </summary>
        /// <param name="a2b"> a transform dictionary indicating start and end joint transforms for the avatar and reference characters respectively</param>
        /// <param name="durations"> list of durations for interpolating each joint. If null, the fallback slerp duration variable is used</param>
        private IEnumerator SlerpJointsSequentially(List<(Transform from, Transform to)> a2b, float[] durations = null)
        {
            int i = 0;
            foreach ((Transform a, Transform b) in a2b)
            {
                Debug.Log($"Started Slerping <size=15><b>{poseJoints[i]}</b></size> joint");
                yield return StartCoroutine(SlerpJointCoroutine(a, b, durations != null && i < durations.Length ? durations[i] : slerpDurationSecondsFallback));
                Debug.Log($"Finished Slerping <size=15><b>{poseJoints[i]}</b></size> joint");
                i++;
            }

            Debug.Log("Finished Slerping <b>all</b> <color=#ff0000>joints</color>");
            SlerpJointsSequentiallyCoroutine = null;
        }

        /// <summary>
        /// maps the avatar's joint to the reference joint using quaternion slerp
        /// </summary>
        /// <param name="index">Index of pose joint</param>
        /// <param name="a">Quaternion to slerp From</param>
        /// <param name="b">Quaternion to slerp To</param>
        /// <param name="duration">the duration which the slerping process takes</param>
        private IEnumerator SlerpJointCoroutine(Transform a, Transform b, float duration)
        {
            float timeCount = 0f;
            Quaternion startRot = a.localRotation;
            Quaternion endRot = b.localRotation;

            while (timeCount < duration)
            {
                if (startRot == endRot) yield break;

                if (SkipJointSlerpKey.wasPressedThisFrame) break;

                timeCount += Time.deltaTime;
                a.localRotation = Quaternion.Slerp(startRot, endRot, timeCount / duration);
                yield return null;
            }

            a.localRotation = endRot;
        }

        // summary: checks whether the new model pose is different from the last stored one.
        // pose: the current model pose captured
        // return: true if pose is equal to last model pose saved, otherwise false
        // edge cases: when no previous poses exist to compare against, the function returns true to allow for adding poses
        private bool IsPosesEqual(PoseModelData pose)
        {
            Quaternion[] modelBoneDirs = pose.avBoneOrientations;
            if (alSavedPoses.Count == 0) return true;

            Quaternion[] lastStoredBoneDirs = alSavedPoses.Last().avBoneOrientations;


            for (int i = 0; i < pose.avBoneOrientations.Length; i++)
            {
                Quaternion modelBoneDir = pose.avBoneOrientations[i];
                Quaternion lastStoredBoneDir = lastStoredBoneDirs[i];
                if (Quaternion.Angle(lastStoredBoneDir, modelBoneDir) != 0) return false;
            }

            Debug.Log("Poses are equal, no need for redundant model poses");
            return true;
        }

        // gets the current avatar pose
        private void GetAvatarPose()
        {
            KinectManager kinectManager = KinectManager.Instance;
            if (kinectManager == null || avatarModel == null || poseJoints == null)
                return;

            if (poseAvatar.avBoneOrientations == null)
            {
                poseAvatar.avBoneOrientations = new Quaternion[poseJoints.Count];
            }

            for (int i = 0; i < poseJoints.Count; i++)
            {
                KinectInterop.JointType joint = poseJoints[i];
                Transform jointTransform = avatarModel.GetBoneTransform(PoseModelHelper.GetBoneIndexByJoint(joint, IsMirrored));
                Quaternion jointOrientation = jointTransform.localRotation;
                poseAvatar.avBoneOrientations[i] = jointOrientation;
                Debug.DrawRay(jointTransform.position, jointOrientation.eulerAngles.normalized, Color.green);
            }
        }

        private Quaternion GetJointOrientation(PoseModelHelper model, KinectInterop.JointType joint, bool IsMirrored, bool isLocal)
        {
            Transform jointTransform = model.GetBoneTransform(PoseModelHelper.GetBoneIndexByJoint(joint, IsMirrored));

            if (isLocal)
            {
                Quaternion qJoint = jointTransform.localRotation;
                Debug.DrawRay(jointTransform.position, qJoint.eulerAngles.normalized, Color.green);

                return jointTransform.localRotation;
            }
            else
            {
                Quaternion qJoint = jointTransform.rotation;
                Debug.DrawRay(jointTransform.position, qJoint.eulerAngles.normalized, Color.green);
                return jointTransform.rotation;
            }

        }

        /// <summary>
        /// Strips out the twist component around the given axis from quaternion q.
        /// </summary>
        /// <param name="q">Original rotation (in local space of the bone).</param>
        /// <param name="axis">Normalized axis to remove twist around.</param>
        /// <returns>Quaternion containing only the swing component.</returns>
        public static void RemoveTwist(Quaternion q, Vector3 axis, out Quaternion swing, out Quaternion twist)
        {
            // Extract vector part
            Vector3 r = new Vector3(q.x, q.y, q.z);
            // Project onto twist axis
            Vector3 proj = Vector3.Dot(r, axis) * axis;

            // Reconstruct the twist quaternion
            twist = new Quaternion(proj.x, proj.y, proj.z, q.w);
            twist = Quaternion.Normalize(twist);

            // Swing is the residual
            swing = q * Quaternion.Inverse(twist);
        }

        // gets the difference between the avatar pose and the list of saved poses
        private void GetPoseDifference(bool IsMirrored)
        {
            if (poseJoints.Count == 0)
                return;

            if (sbDebug != null)
            {
                sbDebug.Clear();
                sbDebug.AppendLine();
            }

            float totalWeighedMatch = 0f;

            for (int i = 0; i < poseJoints.Count; i++)
            {
                //Replace with obtaining from the original reference
                Quaternion qPoseBone = GetJointOrientation(poseModel, poseJoints[i], IsMirrored, true);
                Quaternion qAvatarBone = GetJointOrientation(avatarModel, poseJoints[i], IsMirrored, true);
                
                float fDiff = Quaternion.Angle(qPoseBone, qAvatarBone);
                //get vector along bone if exists
                Transform startBoneTransform = avatarModel.GetBoneTransform(PoseModelHelper.GetBoneIndexByJoint(poseJoints[i], IsMirrored));
                Transform endBoneTransform = avatarModel.GetBoneTransform(PoseModelHelper.GetBoneIndexByJoint(KinectInterop.GetNextJoint(poseJoints[i]), IsMirrored));
                if (startBoneTransform != endBoneTransform)
                {
                    Vector3 boneDirection = (endBoneTransform.position - startBoneTransform.position).normalized;
                    //decompose avatar's joint quaternion
                    RemoveTwist(qAvatarBone, boneDirection, out Quaternion AvatarSwing, out Quaternion AvatarTwist);
                    RemoveTwist(qPoseBone, boneDirection, out Quaternion ReferenceSwing, out Quaternion ReferenceTwist);
                    float newfDiff = Quaternion.Angle(ReferenceSwing, AvatarSwing);
                    Debug.Log($"Previous Difference: {fDiff}\n Swing Difference: {newfDiff}\n Is Better? - <b>{fDiff > newfDiff}</b>\n");
                    fDiff = newfDiff;
                }

                int maxAngle;
                float jointWeight;

                if (!joint2WeightAndMaxAngles.TryGetValue(poseJoints[i], out JointInfo jointInfo))
                {
                    maxAngle = 90;
                    jointWeight = 1.0f / poseJoints.Count;
                    throw new InvalidOperationException("The joint information list is missing some keys");
                }
                else
                {
                    maxAngle = jointInfo.MaxAngle;
                    jointWeight = jointInfo.Weight;
                }

                fDiff = Mathf.Clamp(fDiff, 0, maxAngle);

                float weighedMatch = (1 - (fDiff / maxAngle)) * jointWeight;
                totalWeighedMatch += weighedMatch;

                if (sbDebug != null)
                {
                    sbDebug.AppendFormat("{0} - angle diff: {1:F0}, Max Angle: {4}, match: {2:F0}%, total Effect: {3:F2}", poseJoints[i], fDiff, (1f - fDiff / maxAngle) * 100f, weighedMatch, maxAngle);
                    sbDebug.AppendLine();
                }
            }

            float matchChange = totalWeighedMatch - fMatchPercent;
            fMatchPercent = AccuracySettleTimeSeconds == 0 ? totalWeighedMatch : fMatchPercent + matchChange * Time.deltaTime * AccuracySettleTimeSeconds;
            bPoseMatched = fMatchPercent >= matchThreshold;
        }

    }
}
