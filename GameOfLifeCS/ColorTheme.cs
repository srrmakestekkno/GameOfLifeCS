namespace GameOfLifeCS
{
    public record ColorTheme(string Name, Color Background, Color Cell, Color Grid)
    {
        public static readonly ColorTheme Classic = new(
            "Classic",
            Color.White,
            Color.Black,
            Color.LightGray
        );

        public static readonly ColorTheme Retro80s = new(
            "80s Retro",
            Color.FromArgb(20, 12, 28),      // Dark purple background
            Color.FromArgb(255, 0, 255),      // Bright magenta cells
            Color.FromArgb(100, 50, 150)      // Purple grid
        );

        public static readonly ColorTheme CyberPunk = new(
            "Cyberpunk",
            Color.FromArgb(10, 10, 30),       // Dark blue background
            Color.FromArgb(0, 255, 255),      // Cyan cells
            Color.FromArgb(255, 0, 128)       // Hot pink grid
        );

        public static readonly ColorTheme Matrix = new(
            "Matrix",
            Color.Black,
            Color.FromArgb(0, 255, 65),       // Green cells
            Color.FromArgb(0, 100, 30)        // Dark green grid
        );

        public static readonly ColorTheme Sunset = new(
            "Sunset",
            Color.FromArgb(25, 25, 50),       // Deep blue background
            Color.FromArgb(255, 100, 50),     // Orange cells
            Color.FromArgb(100, 50, 100)      // Purple grid
        );

        public static readonly ColorTheme NeonGreen = new(
            "Neon Green",
            Color.FromArgb(15, 15, 15),       // Almost black
            Color.FromArgb(57, 255, 20),      // Bright neon green
            Color.FromArgb(30, 100, 20)       // Dim green grid
        );

        public static readonly ColorTheme Amber = new(
            "Amber Terminal",
            Color.Black,
            Color.FromArgb(255, 176, 0),      // Amber
            Color.FromArgb(100, 70, 0)        // Dark amber grid
        );

        public static ColorTheme[] AllThemes => 
        [
            Classic,
            Retro80s,
            CyberPunk,
            Matrix,
            Sunset,
            NeonGreen,
            Amber
        ];
    }
}
