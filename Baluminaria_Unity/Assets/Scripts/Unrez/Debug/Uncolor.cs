using System.Runtime.CompilerServices;

namespace Unrez
{
    public class Uncolor
    {
        public string colorHex;
        public Uncolor(string hex)
        {
            colorHex = hex;
        }
        public static Uncolor White
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("FFFFFF");
            }
        }

        public static Uncolor Black
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("000000");
            }
        }

        public static Uncolor Gray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("808080");
            }
        }
        public static Uncolor Red
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("FF0000");
            }
        }

        public static Uncolor Blue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("0000FF");
            }
        }
        public static Uncolor Green
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("00FF00");
            }
        }
        public static Uncolor Magenta
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("FF00FF");
            }
        }
        public static Uncolor Cyan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("FFFF00");
            }
        }
        public static Uncolor Yellow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("00FFFF");
            }
        }
        public static Uncolor Orange
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("FFA500");
            }
        }
        public static Uncolor Pink
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("FFC0CB");
            }
        }
        public static Uncolor CocaCola
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Uncolor("ed1c16");
            }
        }
    }
}