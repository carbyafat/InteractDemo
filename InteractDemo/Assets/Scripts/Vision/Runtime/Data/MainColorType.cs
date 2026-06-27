namespace InteractDemo.Vision
{
    /// <summary>
    /// Fine-grained color categories produced by image color analysis.
    /// </summary>
    public enum MainColorType
    {
        /// <summary>Strong red hue.</summary>
        Red,

        /// <summary>Orange hue between red and yellow.</summary>
        Orange,

        /// <summary>Yellow hue.</summary>
        Yellow,

        /// <summary>Yellow-green hue.</summary>
        Lime,

        /// <summary>Green hue.</summary>
        Green,

        /// <summary>Blue-green hue.</summary>
        Cyan,

        /// <summary>Light blue hue.</summary>
        SkyBlue,

        /// <summary>Blue hue.</summary>
        Blue,

        /// <summary>Purple hue.</summary>
        Purple,

        /// <summary>Red-purple hue.</summary>
        Magenta,

        /// <summary>Pink hue.</summary>
        Pink,

        /// <summary>Dark low-saturation orange or yellow color.</summary>
        Brown,

        /// <summary>Light low-saturation orange or yellow color.</summary>
        Beige,

        /// <summary>Very bright low-saturation color.</summary>
        White,

        /// <summary>Bright low-saturation gray.</summary>
        LightGray,

        /// <summary>Mid low-saturation gray.</summary>
        Gray,

        /// <summary>Dark low-saturation gray.</summary>
        DarkGray,

        /// <summary>Very dark color.</summary>
        Black,

        /// <summary>No meaningful opaque pixels were found.</summary>
        Transparent,

        /// <summary>No single color category is dominant enough.</summary>
        Mixed,

        /// <summary>Fallback when analysis cannot classify the input.</summary>
        Unknown
    }
}
