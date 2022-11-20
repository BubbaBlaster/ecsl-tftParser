using System;
using System.Drawing;

namespace Agora.Forms
{
    /// <summary>
    /// See https://dls.slb.com/guidelines/color/ for information on Color usage
    /// </summary>
    public static class Colors
    {
        public static Color Transparent = Color.FromArgb(0, 0, 0, 0);

        /// DLS Standard Action Blue
        public static Color Action_VoxBlue = Color.FromArgb(0x16, 0x83, 0xFB),
                            Action_VossBlue = Color.FromArgb(0x13, 0x6F, 0xD5);

        /// DLS Standard Background GreyScale
        public static Color BG_Grey01 = Color.FromArgb(0x2D, 0x34, 0x3D),
                            BG_Grey02 = Color.FromArgb(0x39, 0x41, 0x4D),
                            BG_Grey03 = Color.FromArgb(0x41, 0x49, 0x54),
                            BG_Grey04 = Color.FromArgb(0x5e, 0x66, 0x70),
                            BG_Grey05 = Color.FromArgb(0x99, 0xA6, 0xB5),
                            BG_Grey06 = Color.FromArgb(0xC7, 0xD0, 0xD8),
                            BG_Grey07 = Color.FromArgb(0xE8, 0xEC, 0xF2),
                            BG_Grey08 = Color.FromArgb(0xF1, 0xF4, 0xF9),
                            BG_White = Color.FromArgb(0xFF, 0xFF, 0xFF);

        /// DLS Standard Light mode Grey Scale
        public static Color LM_Grey02 = Color.FromArgb(0x39, 0x41, 0x4D),
                            LM_Grey05 = Color.FromArgb(0x99, 0xA6, 0xB5),
                            LM_Grey06 = Color.FromArgb(0xC7, 0xD0, 0xD8),
                            LM_Grey07 = Color.FromArgb(0xE8, 0xEC, 0xF2),
                            LM_Grey08 = Color.FromArgb(0xF1, 0xF4, 0xF9),
                            LM_White = Color.FromArgb(0xFF, 0xFF, 0xFF);

        /// DLS Standard Dark mode Grey Scale
        public static Color DM_Grey01 = Color.FromArgb(0x2D, 0x34, 0x3D),
                            DM_Grey02 = Color.FromArgb(0x39, 0x41, 0x4D),
                            DM_Grey03 = Color.FromArgb(0x41, 0x49, 0x54),
                            DM_Grey04 = Color.FromArgb(0x5e, 0x66, 0x70),
                            DM_Grey05 = Color.FromArgb(0x99, 0xA6, 0xB5),
                            DM_White = Color.FromArgb(0xFF, 0xFF, 0xFF);

        /// DLS Standard Alert Color - Use for warnings or that something is critical and needs action.
        public static Color Alert_Red = Color.FromArgb(0xFF, 0x5A, 0x5A);
        /// DLS Standard Alert Color - Use to show that information may require attention if progress
        /// continues as is.
        public static Color Alert_Yellow = Color.FromArgb(0xFF, 0xDF, 0x22);
        /// DLS Standard Alert Color - Use to show that progress is being made accordingly, everything
        /// is going as planned, and/or a task is approved or completed.
        public static Color Alert_Green = Color.FromArgb(0x47, 0xB2, 0x80);
        /// DLS Standard Alert Color - Use to show that no action to take by the user and that the 
        /// information being displayed is running according to anticipated plan.
        public static Color Alert_BlueGrey = Color.FromArgb(0x5A, 0x77, 0x93);

        /// DLS Standard Chart Colors
        public static Color Chart_Red = Color.FromArgb(0xF4, 0x43, 0x46),
                            Chart_Pink = Color.FromArgb(0xE9, 0x1E, 0x63),
                            Chart_Purple = Color.FromArgb(0x9C, 0x27, 0xB0),
                            Chart_DeepPurple = Color.FromArgb(0x67, 0x3A, 0xB7),
                            Chart_Indigo = Color.FromArgb(0x3F, 0x51, 0xB5),
                            Chart_Blue = Color.FromArgb(0x21, 0x96, 0xF3),
                            Chart_LightBlue = Color.FromArgb(0x03, 0xA9, 0xF4),
                            Chart_Cyan = Color.FromArgb(0x00, 0xBC, 0xD4),
                            Chart_Teal = Color.FromArgb(0x00, 0x96, 0x88),
                            Chart_Green = Color.FromArgb(0x4C, 0xAF, 0x50),
                            Chart_LightGreen = Color.FromArgb(0x8B, 0xC3, 0x4A),
                            Chart_Lime = Color.FromArgb(0xCD, 0xDC, 0x39),
                            Chart_Yellow = Color.FromArgb(0xFF, 0xEB, 0x3B),
                            Chart_Amber = Color.FromArgb(0xFF, 0xC1, 0x07),
                            Chart_Orange = Color.FromArgb(0xFF, 0x98, 0x00),
                            Chart_DeepOrange = Color.FromArgb(0xFF, 0x57, 0x22),
                            Chart_Brown = Color.FromArgb(0x79, 0x55, 0x48);

        /// Provides either a Black or White Color that Contrasts most with Color given.
        public static Color ContrastColor(Color color)
        {
            // Counting the perceptive luminance - human eye favors green color... 
            double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            return luminance > 0.5 ? Color.Black : Color.White;
        }

        static public Color ColorFromString(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte red = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
            byte green = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
            byte blue = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
            return Color.FromArgb(red, green, blue);
        }
    }
}
