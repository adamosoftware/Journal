using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AdamOneilSoftware
{
    internal class PanelHandler
    {
        private bool _toggled = false;
        private GridLength _width = new GridLength(0);
        private ColumnDefinition _column = null;
        private MenuItem _togglerMenu = null;

        public MenuItem TogglerMenu
        {
            get { return _togglerMenu; }
            set
            {
                _togglerMenu = value;
                _togglerMenu.Click += togglerMenu_Click;
            }
        }

        private void togglerMenu_Click(object sender, RoutedEventArgs e)
        {
            Toggle();
        }

        public PanelHandler(GridSplitter splitter, ColumnDefinition gridColumn)
        {
            splitter.DragCompleted += Splitter_DragCompleted;
            _column = gridColumn;
        }

        private void Splitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (_toggled) return;
            _width = _column.Width;
            if (TogglerMenu != null) TogglerMenu.IsChecked = (_width.Value > 0);
        }

        public void Collapse()
        {
            _width = _column.Width; // note the width it is now
            _column.Width = new GridLength(0);
        }

        public void Expand()
        {
            _column.Width = _width;
        }

        public void Toggle()
        {
            _toggled = true;
            if (_column.Width.Value > 0)
            {
                _column.Width = new GridLength(0);
                if (TogglerMenu != null) TogglerMenu.IsChecked = false;
            }
            else
            {
                _column.Width = _width;
                if (TogglerMenu != null) TogglerMenu.IsChecked = true;
            }
            _toggled = false;
        }
    }
}