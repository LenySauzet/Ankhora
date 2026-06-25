using System;
using UnityEngine;

namespace Ankhora.Domain.Model
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

        /// <summary>
        /// True when this is the (0,0) sentinel, i.e. the pin is visible for the whole chapter with
        /// no explicit window. Call sites should use this instead of comparing against (0,0) by hand.
        /// </summary>
        public bool IsWholeChapter => start == 0f && end == 0f;
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
