﻿namespace AvalonStudio.TextEditor.Rendering
{
    using Perspex;
    using Perspex.Controls;
    using System;

    /// <summary>
    /// An enumeration of well-known layers.
    /// </summary>
    public enum KnownLayer
    {
        /// <summary>
        /// This layer is in the background.
        /// There is no UIElement to represent this layer, it is directly drawn in the TextView.
        /// It is not possible to replace the background layer or insert new layers below it.
        /// </summary>
        /// <remarks>This layer is below the Selection layer.</remarks>
        Background,
        /// <summary>
        /// This layer contains the selection rectangle.
        /// </summary>
        /// <remarks>This layer is between the Background and the Text layers.</remarks>
        Selection,
        /// <summary>
        /// This layer contains the text and inline UI elements.
        /// </summary>
        /// <remarks>This layer is between the Selection and the Caret layers.</remarks>
        Text,
        /// <summary>
        /// This layer contains the blinking caret.
        /// </summary>
        /// <remarks>This layer is above the Text layer.</remarks>
        Caret
    }

    /// <summary>
    /// Specifies where a new layer is inserted, in relation to an old layer.
    /// </summary>
    public enum LayerInsertionPosition
    {
        /// <summary>
        /// The new layer is inserted below the specified layer.
        /// </summary>
        Below,
        /// <summary>
        /// The new layer replaces the specified layer. The old layer is removed
        /// from the <see cref="TextView.Layers"/> collection.
        /// </summary>
        Replace,
        /// <summary>
        /// The new layer is inserted above the specified layer.
        /// </summary>
        Above
    }

    sealed class LayerPosition : PerspexObject, IComparable<LayerPosition>
    {
        internal static readonly PerspexProperty LayerPositionProperty =
            PerspexProperty.Register<Layer, LayerPosition>("LayerPosition");

        public static void SetLayerPosition(Control layer, LayerPosition value)
        {
            layer.SetValue(LayerPositionProperty, value);
        }

        public static LayerPosition GetLayerPosition(IControl layer)
        {
            return (LayerPosition)layer.GetValue(LayerPositionProperty);
        }

        internal readonly KnownLayer KnownLayer;
        internal readonly LayerInsertionPosition Position;

        public LayerPosition(KnownLayer knownLayer, LayerInsertionPosition position)
        {
            this.KnownLayer = knownLayer;
            this.Position = position;
        }

        public int CompareTo(LayerPosition other)
        {
            int r = this.KnownLayer.CompareTo(other.KnownLayer);
            if (r != 0)
                return r;
            else
                return this.Position.CompareTo(other.Position);
        }
    }
}
