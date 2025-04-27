using com.rfilkov.components;
using com.rfilkov.kinect;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BodyJointsInfo", menuName = "Scriptable Objects/BodyJointsInfo")]
public class BodyJointsInfo : ScriptableObject
{
    [SerializeField] NewDict JointMaxAngleList;

    private Dictionary<KinectInterop.JointType, int> _joint2MaxAngle;
    public Dictionary<KinectInterop.JointType, int> Joint2MaxAngle
    {
        get
        {
            _joint2MaxAngle ??= JointMaxAngleList.ToDictionary();

            return _joint2MaxAngle;
        }

        private set { _joint2MaxAngle = value; }
    }

    [Serializable]
    class NewDict
    {
        [SerializeField] DictItem[] items;

        public Dictionary<KinectInterop.JointType, int> ToDictionary()
        {
            Dictionary<KinectInterop.JointType, int> joint2MaxAngle = new();
            foreach (var item in items)
            {
                joint2MaxAngle.Add(item.JointType, item.JointMaxAngle);
                KinectInterop.JointType mirroredJoint = PoseModelHelper.mirrorJointMap2JointMap[item.JointType];
                if (mirroredJoint != item.JointType) joint2MaxAngle.Add(mirroredJoint, item.JointMaxAngle);
            }

            return joint2MaxAngle;
        }
    }

    [Serializable]
    class DictItem
    {
        public KinectInterop.JointType JointType;
        public int JointMaxAngle;
    }
}
