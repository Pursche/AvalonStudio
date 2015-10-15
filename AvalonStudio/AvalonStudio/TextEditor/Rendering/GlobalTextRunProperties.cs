﻿namespace AvalonStudio.TextEditor.Rendering
{
    using Perspex.Media;
    using Perspex.Media.TextFormatting;

    sealed class GlobalTextRunProperties : TextRunProperties
    {
        //internal Typeface typeface;
        internal double fontRenderingEmSize;
        internal Brush foregroundBrush;
        internal Brush backgroundBrush;
        internal System.Globalization.CultureInfo cultureInfo;

        //public override Typeface Typeface { get { return typeface; } }
        public override double FontRenderingEmSize { get { return fontRenderingEmSize; } }
        public override double FontHintingEmSize { get { return fontRenderingEmSize; } }
        //public override TextDecorationCollection TextDecorations { get { return null; } }
        public override Brush ForegroundBrush { get { return foregroundBrush; } }
        public override Brush BackgroundBrush { get { return backgroundBrush; } }
        public override System.Globalization.CultureInfo CultureInfo { get { return cultureInfo; } }
        //public override TextEffectCollection TextEffects { get { return null; } }
    }
}
