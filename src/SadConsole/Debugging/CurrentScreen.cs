﻿#if XNA
using Microsoft.Xna.Framework;
#endif

using SadConsole.Controls;
using System.Reflection;
using System.Linq;
using SadConsole.Themes;
using System;
using SadConsole.Input;

namespace SadConsole.Debug
{
    public static class CurrentScreen
    {
        private class DebugWindow : Window
        {
            private Console _originalScreen;
            private ContainerConsole _wrapperScreen;
            private ConsoleStack _inputConsoles;

            private Label _labelConsoleTitle;
            private Label _labelConsoleWidth;
            private Label _labelConsoleHeight;
            private ListBox _listConsoles;
            private CheckBox _checkIsVisible;
            private ScrollingSurfaceView _surfaceView;

            private bool _isReadingConsole;

            public DebugWindow(Font font) : base (78, 22, font)
            {
                Theme = Theme.Clone();
                Theme.ListBoxTheme.DrawBorder = true;

                Title = "Global.CurrentScreen Debugger";
                IsModalDefault = true;
                CloseOnEscKey = true;

                _listConsoles = new ListBox(30, 15) { Position = new Point(2, 3) };
                _listConsoles.SelectedItemChanged += Listbox_SelectedItemChanged;
                Add(_listConsoles);

                var label = CreateLabel("Current Screen", new Point(_listConsoles.Bounds.Left, _listConsoles.Bounds.Top - 1));
                label = CreateLabel("Selected Console: ", new Point(_listConsoles.Bounds.Right + 1, label.Bounds.Top));
                {
                    _labelConsoleTitle = new Label(Width - label.Bounds.Right - 1) { Position = new Point(label.Bounds.Right, label.Bounds.Top) };
                    Add(_labelConsoleTitle);
                }

                label = CreateLabel("Width: ", new Point(label.Bounds.Left, label.Bounds.Bottom + 1));
                {
                    _labelConsoleWidth = new Label(5) { Position = new Point(label.Bounds.Right, label.Bounds.Top) };
                    _labelConsoleWidth.Alignment = HorizontalAlignment.Right;
                    Add(_labelConsoleWidth);
                }

                label = CreateLabel("Height: ", new Point(_labelConsoleWidth.Bounds.Right + 1, _labelConsoleWidth.Bounds.Top));
                {
                    _labelConsoleHeight = new Label(5) { Position = new Point(label.Bounds.Right, label.Bounds.Top) };
                    _labelConsoleHeight.Alignment = HorizontalAlignment.Right;
                    Add(_labelConsoleHeight);
                }

                _checkIsVisible = new CheckBox(15, 1);
                _checkIsVisible.Text = "Is Visible";
                _checkIsVisible.Position = new Point(_listConsoles.Bounds.Right + 1, label.Bounds.Bottom);
                _checkIsVisible.IsSelectedChanged += _checkIsVisible_IsSelectedChanged;
                Add(_checkIsVisible);

                _surfaceView = new ScrollingSurfaceView(Width - 3 - _listConsoles.Bounds.Right + 1, Height - 2 - _checkIsVisible.Bounds.Bottom + 1);
                _surfaceView.Theme = new ScrollingSurfaceView.ScrollingSurfaceViewTheme();
                _surfaceView.Position = new Point(_listConsoles.Bounds.Right + 1, _checkIsVisible.Bounds.Bottom);
                Add(_surfaceView);

                // Position EDIT
                // Map View of surface
                


                // Cell peek of surface
                // Cell edit of surface
                // Save surface

                //label = CreateLabel("Visible: ", new Point(listbox.Bounds.Right + 1, _labelConsoleWidth.Bounds.Top));
                //{
                //    _labelConsoleHeight = new Label(5) { Position = new Point(label.Bounds.Right, label.Bounds.Top) };
                //    Add(_labelConsoleHeight);
                //}

                Label CreateLabel(string text, Point position)
                {
                    var labelTemp = new Label(text) { Position = position, TextColor = Theme.Colors.TitleText };
                    Add(labelTemp);
                    return labelTemp;
                }


                // Setup the "steal the focus" system to pause the existing current screen
                _originalScreen = Global.CurrentScreen;
                _wrapperScreen = new ContainerConsole();
                _inputConsoles = Global.FocusedConsoles;
                Global.FocusedConsoles = new ConsoleStack();
                
                AddConsoleToList(_originalScreen);
                void AddConsoleToList(Console console, int indent = 0)
                {
                    var debugger = console.GetType().GetTypeInfo().GetCustomAttributes<System.Diagnostics.DebuggerDisplayAttribute>().FirstOrDefault();
                    var text = debugger != null ? debugger.Value : console.ToString();

                    _listConsoles.Items.Add(new ConsoleListboxItem(new string('-', indent) + text, console));

                    foreach (var child in console.Children)
                        AddConsoleToList(child, indent + 1);
                }

                _wrapperScreen.Children.Add(_originalScreen);
                _wrapperScreen.IsPaused = true;
                Global.CurrentScreen = new ContainerConsole();
                Global.CurrentScreen.Children.Add(_wrapperScreen);

                _listConsoles.SelectedItem = _listConsoles.Items[0];
            }

            private void _checkIsVisible_IsSelectedChanged(object sender, System.EventArgs e)
            {
                if (_isReadingConsole) return;

                ((ConsoleListboxItem)_listConsoles.SelectedItem).Console.IsVisible = ((CheckBox)sender).IsSelected;
            }

            private void Listbox_SelectedItemChanged(object sender, ListBox.SelectedItemEventArgs e)
            {
                var item = (ConsoleListboxItem)e.Item;
                _isReadingConsole = true;
                _labelConsoleTitle.DisplayText = item.ToString().Trim('-');
                _labelConsoleWidth.DisplayText = item.Console.Width.ToString();
                _labelConsoleHeight.DisplayText = item.Console.Height.ToString();
                _checkIsVisible.IsSelected = item.Console.IsVisible;
                _isReadingConsole = false;
                _surfaceView.SetTargetSurface(item.Console);
            }

            public override string ToString()
            {
                return "Debug Window";
            }

            public override void Hide()
            {
                base.Hide();
                _originalScreen.Parent = null;
                Global.CurrentScreen = _originalScreen;
                _originalScreen = null;
                _wrapperScreen = null;
                Global.FocusedConsoles = _inputConsoles;
            }
        }

        public static void Show() => Show(Global.FontDefault);

        public static void Show(Font font)
        {
            DebugWindow window = new DebugWindow(font);
            window.Show();
            window.Center();
        }

        //private class DebugSurface : Console
        //{
        //    public override void Draw(TimeSpan timeElapsed)
        //    {
        //        base.Draw(timeElapsed);
        //    }
        //}

        //public static void Show()
        //{

        //}

        private class ConsoleListboxItem
        {
            private string Title;
            public Console Console;

            public ConsoleListboxItem(string title, Console console)
            {
                Console = console;
                Title = title;
            }

            public override string ToString() => Title;
        }

        private class ScrollingSurfaceView : ControlBase
        {
            protected ScrollBar HorizontalBar;
            protected ScrollBar VerticalBar;

            protected CellSurface SurfaceReference;
            protected ScrollingConsole SurfaceView;

            protected int HorizontalBarY;
            protected int VerticalBarX;

            public ScrollingSurfaceView(int width, int height) : base(width, height)
            {
                HorizontalBar = new ScrollBar(Orientation.Horizontal, width - 1);
                VerticalBar = new ScrollBar(Orientation.Vertical, height - 1);

                HorizontalBar.Position = new Point(0, height - 1);
                VerticalBar.Position = new Point(width - 1, 0);

                HorizontalBar.ValueChanged += HorizontalBar_ValueChanged;
                VerticalBar.ValueChanged += VerticalBar_ValueChanged;

                HorizontalBar.IsEnabled = false;
                VerticalBar.IsEnabled = false;
                HorizontalBar.IsVisible = false;
                VerticalBar.IsVisible = false;
            }

            private void VerticalBar_ValueChanged(object sender, EventArgs e)
            {
                if (!((ScrollBar)sender).IsEnabled) return;

                SurfaceView.ViewPort = new Rectangle(SurfaceView.ViewPort.X, ((ScrollBar)sender).Value, SurfaceView.ViewPort.Width, SurfaceView.ViewPort.Height);
                IsDirty = true;
            }

            private void HorizontalBar_ValueChanged(object sender, EventArgs e)
            {
                if (!((ScrollBar)sender).IsEnabled) return;

                SurfaceView.ViewPort = new Rectangle(((ScrollBar)sender).Value, SurfaceView.ViewPort.Y, SurfaceView.ViewPort.Width, SurfaceView.ViewPort.Height);
                IsDirty = true;
            }

            protected override void OnParentChanged()
            {
                VerticalBar.Parent = parent;
                HorizontalBar.Parent = parent;
            }

            protected override void OnPositionChanged()
            {
                VerticalBarX = Width - 1;
                HorizontalBarY = Height - 1;
                VerticalBar.Position = Position + new Point(VerticalBarX, 0);
                HorizontalBar.Position = Position + new Point(0, HorizontalBarY);
            }

            public void SetTargetSurface(Console surface)
            {
                SurfaceReference = null;
                SurfaceView = null;

                SurfaceReference = surface;
                SurfaceView = new ScrollingConsole(surface.Width, surface.Height, surface.Font, new Rectangle(0, 0, Width - 1 > surface.Width ? surface.Width : Width - 1,
                                                                                                                    Height - 1 > surface.Height ? surface.Height : Height - 1), surface.Cells);

                if (SurfaceView.ViewPort.Width != SurfaceView.Width)
                {
                    HorizontalBar.IsEnabled = true;
                    HorizontalBar.Maximum = SurfaceView.Width - SurfaceView.ViewPort.Width;
                }
                else
                    HorizontalBar.IsEnabled = false;

                if (SurfaceView.ViewPort.Height != SurfaceView.Height)
                {
                    VerticalBar.IsEnabled = true;
                    VerticalBar.Maximum = SurfaceView.Height - SurfaceView.ViewPort.Height;
                }
                else
                    VerticalBar.IsEnabled = false;

                VerticalBar.Value = 0;
                HorizontalBar.Value = 0;

                IsDirty = true;
            }

            public override bool ProcessMouse(MouseConsoleState state)
            {
                if (isEnabled)
                {
                    if (isMouseOver)
                    {
                        var mouseControlPosition = TransformConsolePositionByControlPosition(state.CellPosition);

                        if (mouseControlPosition.X == VerticalBarX)
                        {
                            VerticalBar.ProcessMouse(state);
                        }

                        if (mouseControlPosition.Y == HorizontalBarY)
                        {
                            HorizontalBar.ProcessMouse(state);
                        }
                    }
                    else
                        base.ProcessMouse(state);
                }

                return false;
            }

            public class ScrollingSurfaceViewTheme : Themes.ThemeBase
            {
                public override void Attached(ControlBase control)
                {
                    control.Surface = new CellSurface(control.Width, control.Height);

                    ((ScrollingSurfaceView)control).VerticalBar.Theme = new ScrollBarTheme();
                    ((ScrollingSurfaceView)control).HorizontalBar.Theme = new ScrollBarTheme();

                    base.Attached(control);
                }

                public override void UpdateAndDraw(ControlBase control, TimeSpan time)
                {
                    if (!(control is ScrollingSurfaceView scroller)) return;

                    if (!scroller.IsDirty) return;

                    if (scroller.SurfaceView == null) return;

                    Cell appearance = GetStateAppearance(scroller.State);

                    scroller.Surface.Clear();
                    scroller.SurfaceView.Copy(scroller.SurfaceView.ViewPort, scroller.Surface, 0, 0);

                    if (scroller.SurfaceReference is ControlsConsole controlsConsole)
                    {
                        foreach (var childControl in controlsConsole.Controls)
                        {
                            for (var i = 0; i < childControl.Surface.Cells.Length; i++)
                            {
                                ref var cell = ref childControl.Surface.Cells[i];

                                if (!cell.IsVisible) continue;

                                var cellRenderPosition = i.ToPoint(childControl.Surface.Width) + childControl.Position;

                                if (!scroller.SurfaceView.ViewPort.Contains(cellRenderPosition)) continue;

                                cell.CopyAppearanceTo(scroller.Surface[(cellRenderPosition - scroller.SurfaceView.ViewPort.Location).ToIndex(scroller.Surface.Width)]);
                            }
                        }
                    }

                    scroller.VerticalBar.IsDirty = true;
                    scroller.VerticalBar.Update(time);

                    scroller.HorizontalBar.IsDirty = true;
                    scroller.HorizontalBar.Update(time);

                    for (var y = 0; y < scroller.VerticalBar.Height; y++)
                    {
                        scroller.Surface.SetGlyph(scroller.VerticalBarX, y, scroller.VerticalBar.Surface[0, y].Glyph);
                        scroller.Surface.SetCellAppearance(scroller.VerticalBarX, y, scroller.VerticalBar.Surface[0, y]);
                    }

                    for (var x = 0; x < scroller.HorizontalBar.Width; x++)
                    {
                        scroller.Surface.SetGlyph(x, scroller.HorizontalBarY, scroller.HorizontalBar.Surface[x, 0].Glyph);
                        scroller.Surface.SetCellAppearance(x, scroller.HorizontalBarY, scroller.HorizontalBar.Surface[x, 0]);
                    }

                    scroller.IsDirty = false;
                }

                public override ThemeBase Clone()
                {
                    return new ScrollingSurfaceViewTheme()
                    {
                        Colors = Colors?.Clone(),
                        Normal = Normal.Clone(),
                        Disabled = Disabled.Clone(),
                        MouseOver = MouseOver.Clone(),
                        MouseDown = MouseDown.Clone(),
                        Selected = Selected.Clone(),
                        Focused = Focused.Clone(),
                    };
                }
            }
        }
    }
}
