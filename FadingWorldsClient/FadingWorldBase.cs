using System;
using System.Windows.Forms;

namespace FadingWorldsClient
{
	internal static class FadingWorldBase {
		[STAThread]
		private static void Main() {
			Loader l;
			try {
				Application.Run(l = new Loader());
			}
			catch (Exception ex) {
				MessageBox.Show(ex.ToString());
			}
		}
	}
}