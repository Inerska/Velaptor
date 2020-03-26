﻿using Raptor.Graphics;

namespace Raptor.UI
{
    /// <summary>
    /// Represents a single piece of text rendered to a graphics surface.
    /// </summary>
    public class UIText
    {

        #region Private Fields
        private int _elapsedTime;//The amount of time that has elapsed since the last frame in miliseconds.
        private bool _updateText;//Indicates if the text can be updated.  Only updated if the UpdateFrequency value is >= to the elapsed time
        private GameText _labelText;
        #endregion


        #region Constructors
        /// <summary>
        /// Creates a new instance of <see cref="UIText"/>.
        /// </summary>
        public UIText() => Position = Vector.Zero;


        /// <summary>
        /// Creates a new instance of <see cref="UIText"/>.
        /// </summary>
        /// <param name="position">The position to to render the text item.</param>
        public UIText(Vector position) => Position = position;


        /// <summary>
        /// Creates a new instance of <see cref="UIText"/>.
        /// </summary>
        /// <param name="x">The X location of the text item.</param>
        /// <param name="y">The Y location of the text item.</param>
        public UIText(int x = 0, int y = 0) => Position = new Vector(x, y);
        #endregion


        #region Props
        /// <summary>
        /// Gets or sets a value indicating if the update frequency should be ignored.
        /// </summary>
        public bool IgnoreUpdateFrequency { get; set; } = true;

        /// <summary>
        /// Gets or sets the selected color of the text item.
        /// </summary>
        public GameColor SelectedColor { get; set; } = new GameColor(255, 255, 255, 0);

        /// <summary>
        /// Gets or sets the name of the text item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the label section of the text item.
        /// </summary>
        public GameText LabelText
        {
            get => _labelText;
            set
            {
                value.Text += ": ";
                _labelText = value;
            }
        }

        /// <summary>
        /// Gets or sets the value section of the text.
        /// </summary>
        public GameText ValueText { get; set; }

        /// <summary>
        /// Gets or sets the position of the text on the graphics surface.
        /// </summary>
        public Vector Position { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the text will render as selected.
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        /// Gets or sets the frequency in milliseconds that the text will get updated.
        /// </summary>
        public int UpdateFrequency { get; set; } = 62;

        /// <summary>
        /// Gets or sets the size of the text. <see cref="Vector.X"/> is for the width and <see cref="Vector.Y"/> is for the height.
        /// </summary>
        public Vector TextItemSize => new Vector(Width, Height);

        /// <summary>
        /// Gets the width of the entire text.
        /// </summary>
        public int Width => LabelText.Width + SectionSpacing + ValueText.Width;

        /// <summary>
        /// Gets the height of the entire text item.
        /// </summary>
        public int Height => LabelText.Height > ValueText.Height ? LabelText.Height : ValueText.Height;

        /// <summary>
        /// Gets the location of the right side of the <see cref="UIText"/>.
        /// </summary>
        public int Right => (int)Position.X + Width;

        /// <summary>
        /// Gets the location of the bottom of the <see cref="UIText"/>.
        /// </summary>
        public int Bottom => (int)Position.Y + Height;

        /// <summary>
        /// Adds an additional amount of space to the vertical position of the label section of the text.
        /// </summary>
        public int VerticalLabelOffset { get; set; } = 0;

        /// <summary>
        /// Adds an additional amount of space to the vertical position of the value section of the text.
        /// </summary>
        public int VerticalValueOffset { get; set; } = 0;

        /// <summary>
        /// Gets or sets the spacing between the label and value sections.
        /// </summary>
        public int SectionSpacing { get; set; } = 5;

        /// <summary>
        /// Gets or sets the color of the label section of the text.
        /// </summary>
        public GameColor LabelColor { get; set; } = new GameColor(255, 0, 0, 0);

        /// <summary>
        /// Gets or sets the color of the value section of the text.
        /// </summary>
        public GameColor ValueColor { get; set; } = new GameColor(255, 0, 0, 0);

        /// <summary>
        /// Gets or sets a value indicating if the <see cref="UIText"/> item will render in the
        /// regular color or disabled color.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the forecolor of the <see cref="UIText"/> item when disabled.
        /// </summary>
        public GameColor DisabledForecolor { get; set; } = new GameColor(255, 100, 100, 100);
        #endregion


        #region Public Methods
        /// <summary>
        /// Sets the text of the label section.
        /// </summary>
        /// <param name="text">The text to set the label section to.</param>
        public void SetLabelText(string text)
        {
            if (_updateText || UpdateFrequency == 0 || IgnoreUpdateFrequency)
            {
                LabelText.Text = text;
                _updateText = false;
            }
        }


        /// <summary>
        /// Sets the text of the value section.
        /// </summary>
        /// <param name="text">The text to set the value section to.</param>
        public void SetValueText(string text)
        {
            if (_updateText || UpdateFrequency == 0 || IgnoreUpdateFrequency)
            {
                ValueText.Text = text;
                _updateText = false;
            }
        }


        /// <summary>
        /// Updates the text item. This helps keep the update frequency up to date.
        /// </summary>
        /// <param name="gameTime">The game time of the last frame.</param>
        public void Update(IEngineTiming gameTime)
        {
            _elapsedTime += gameTime.ElapsedEngineTime.Milliseconds;

            if (_elapsedTime >= UpdateFrequency)
            {
                _elapsedTime = 0;
                _updateText = true;
            }
        }


        /// <summary>
        /// Render the text item to the screen.
        /// </summary>
        /// <param name="renderer">The renderer to use to render the <see cref="UIText"/>.</param>
        public void Render(Renderer renderer)
        {
            renderer.Render(LabelText, Position.X, Position.Y + VerticalLabelOffset);
            renderer.Render(ValueText, Position.X + LabelText.Width + SectionSpacing, Position.Y + VerticalValueOffset);
        }
        #endregion
    }
}