using System;
using UnityEngine;

namespace Ankhora.Domain
{
    public enum PinType
    {
        Text,
        Image,
    }

    /// <summary>Optional visibility window on the timeline; (0,0) means "whole chapter".</summary>
    [Serializable]
    public struct TimeRange
    {
        public float start;
        public float end;
    }

    /// <summary>
    /// A spatial annotation placed by the instructor. <see cref="payload"/> is inline UTF-8 text
    /// when <see cref="type"/> is <see cref="PinType.Text"/>, or a relative blob path when Image.
    /// </summary>
    [Serializable]
    public class Pin
    {
        public string id;
        public PinType type;
        public string payload;

        /// <summary>Position + rotation; the panel uses the rotation to face the learner.</summary>
        public Pose pose;

        public TimeRange timeRange;
    }
}
