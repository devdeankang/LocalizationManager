using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using log4net.Appender;

namespace LocalizationManager
{
	public class TextBoxAppender : AppenderSkeleton
	{
		static private TextBox _textBox;
		static public TextBox AppenderTextBox
		{
			get
			{
				return _textBox;
			}
			set
			{
				_textBox = value;
			}
		}

		private static DependencyObject FindDescendant(DependencyObject parent, string name)
		{
			// See if this object has the target name.
			FrameworkElement element = parent as FrameworkElement;
			if ((element != null) && (element.Name == name)) return parent;

			// Recursively check the children.
			int num_children = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < num_children; i++)
			{
				// See if this child has the target name.
				DependencyObject child = VisualTreeHelper.GetChild(parent, i);
				DependencyObject descendant = FindDescendant(child, name);
				if (descendant != null) return descendant;
			}

			// We didn't find a descendant with the target name.
			return null;
		}
		protected override void Append(log4net.Core.LoggingEvent loggingEvent)
		{
			if (_textBox == null)
				return;
			_textBox.Dispatcher.BeginInvoke((Action)(
				() => _textBox.AppendText(RenderLoggingEvent(loggingEvent)))
			);
		}
	}
}
