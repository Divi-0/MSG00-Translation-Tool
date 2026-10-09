namespace MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue
{
    /// <summary>
    /// Script commands of the prologue/epilogue csvb, see docs/prologue/README.md.
    /// Pointers with the same offset form one group and share one offset table slot.
    /// </summary>
    public enum ProloguePointerType
    {
        /// <summary>
        /// Transition before a text or image; 8 byte slot, high 4 bytes = length in frames (60/30/18), low = 0 or 2 (unknown). Hypothesis.
        /// </summary>
        TransitionBefore = 0x00,

        /// <summary>
        /// Transition (fade-out) after an image; same slot layout as <see cref="TransitionBefore"/>. Hypothesis.
        /// </summary>
        TransitionAfter = 0x01,

        /// <summary>
        /// Shows the current image/animation for N frames (4 byte slot); typing of the preceding text runs meanwhile.
        /// A looping animation restarts when N is longer than the animation. Confirmed.
        /// </summary>
        ShowImage = 0x02,

        /// <summary>
        /// First command of the script; 4 byte slot. Meaning unknown.
        /// </summary>
        ScriptStart = 0x03,

        /// <summary>
        /// Loads an image object; 8 byte slot with two text offsets (e.g. "pro_02", "pro_02_obj").
        /// </summary>
        LoadImage = 0x04,

        /// <summary>
        /// Starts a scene change group (with <see cref="SwitchImage"/>, <see cref="ClearText"/>, <see cref="SceneChangeEnd"/>);
        /// the slot points to the name of the next image. Hypothesis.
        /// </summary>
        SceneChange = 0x05,

        /// <summary>
        /// Ends a scene change group. In epilogues it also appears alone with two text offsets. Hypothesis.
        /// </summary>
        SceneChangeEnd = 0x06,

        /// <summary>
        /// Switches to another image; 8 byte slot with text offsets of the image name (e.g. "pro_03").
        /// Groups without <see cref="ClearText"/> keep the text visible (montages).
        /// </summary>
        SwitchImage = 0x07,

        /// <summary>
        /// Starts typing a text; low 4 bytes = text offset, high 4 bytes = 90 - 6 * line count (probably vertical position).
        /// </summary>
        Text = 0x08,

        /// <summary>
        /// Clears the text: the text disappears when a group containing this command is reached. Confirmed.
        /// </summary>
        ClearText = 0x09,

        /// <summary>
        /// Image reference that starts a scene change group like <see cref="SwitchImage"/>; exact difference unknown.
        /// </summary>
        Unknown0A = 0x0A,

        /// <summary>
        /// Only seen in the "pro_07" group. Meaning unknown.
        /// </summary>
        Unknown0B = 0x0B,

        /// <summary>
        /// 4 byte slot, seen after the last image (value 120). Not a wait: it does not extend the scene,
        /// and large values (434+) made the game hang before the prologue. Meaning unknown.
        /// </summary>
        Unknown0C = 0x0C,
    }
}